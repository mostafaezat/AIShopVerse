using Application.Features.OrderFeatures.Commands;
using Application.Features.PaymentFeatures.Commands;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.NotificationEntities;
using Domain.Entities.PaymentEntities;
using Domain.Entities.PromotionEntities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Order
{
    public class CheckoutFlowTests
    {
        private const string ShippingAddress = "123 Test Street";

        private static async Task<Product> SeedProductAsync(
            TestDb db,
            string userId,
            string sku,
            decimal price = 100m,
            int stock = 10,
            int? lowStockThreshold = null)
        {
            var category = new Category
            {
                NameAR = "فئة",
                NameEN = "Category",
                DisplayOrder = 1,
                IsActive = true
            };

            var product = new Product
            {
                NameAR = "منتج",
                NameEN = $"Product {sku}",
                SKU = sku,
                Price = price,
                StockQuantity = stock,
                LowStockThreshold = lowStockThreshold,
                IsActive = true,
                Category = category
            };
            db.DbContext.Products.Add(product);
            await db.DbContext.SaveChangesAsync();

            return product;
        }

        private static async Task AddCartItemAsync(TestDb db, string userId, Product product, int quantity)
        {
            var cart = await db.DbContext.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                db.DbContext.Carts.Add(cart);
            }

            db.DbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = product.DiscountPrice ?? product.Price
            });

            await db.DbContext.SaveChangesAsync();
        }

        private static CheckoutCommand CheckoutRequest(string? couponCode = null)
            => new()
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CashOnDelivery,
                CouponCode = couponCode
            };

        [Fact]
        public async Task Checkout_Success_CreatesOrderAndPaymentAndDecrementsStock()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var product = await SeedProductAsync(db, user.Id, "SKU-CHECKOUT", stock: 10, lowStockThreshold: 3);
            await AddCartItemAsync(db, user.Id, product, 8);

            var result = await db.Mediator.Send(CheckoutRequest());

            Assert.True(result.IsSuccess, result.Message ?? "checkout failed");

            var freshStock = await db.DbContext.Products
                .Where(p => p.Id == product.Id)
                .Select(p => p.StockQuantity)
                .SingleAsync();
            Assert.Equal(2, freshStock);

            var order = await db.DbContext.Orders
                .Include(o => o.Items)
                .SingleAsync();
            Assert.Equal(8, order.Items.Single().Quantity);
            Assert.Equal("Pending", order.Status.ToString());

            var payment = await db.DbContext.Payments.SingleAsync();
            Assert.Equal("CashOnDelivery", payment.Method);
            Assert.Equal("Pending", payment.Status.ToString());
            Assert.Null(payment.TransactionId);

            Assert.Equal(0, await db.DbContext.CartItems.CountAsync());
            Assert.True(await db.DbContext.Notifications.AnyAsync(n => n.Type == NotificationType.Sale),
                "expected a low-stock notification");
        }

        [Fact]
        public async Task Checkout_WithInvalidCoupon_FailsWithoutTouchingStock()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var product = await SeedProductAsync(db, user.Id, "SKU-COUPON", stock: 5);
            await AddCartItemAsync(db, user.Id, product, 1);

            db.DbContext.Coupons.Add(new Coupon
            {
                Code = "INVALID",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                IsActive = false,
                ValidFrom = DateTime.UtcNow.AddDays(-1),
                ValidTo = DateTime.UtcNow.AddDays(1)
            });
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(CheckoutRequest("INVALID"));

            Assert.False(result.IsSuccess, result.Message ?? "expected checkout to fail");
            var stock = await db.DbContext.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync();
            Assert.Equal(5, stock);
            Assert.Equal(0, await db.DbContext.Orders.CountAsync());
            Assert.Equal(0, await db.DbContext.Payments.CountAsync());
            Assert.Equal(1, await db.DbContext.CartItems.CountAsync());
        }

        [Fact]
        public async Task Checkout_WithInsufficientStock_FailsWithNoPartialWrite()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var product = await SeedProductAsync(db, user.Id, "SKU-LOWSTOCK", stock: 1);
            await AddCartItemAsync(db, user.Id, product, 3);

            var result = await db.Mediator.Send(CheckoutRequest());

            Assert.False(result.IsSuccess, result.Message ?? "expected checkout to fail");
            var stock = await db.DbContext.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync();
            Assert.Equal(1, stock);
            Assert.Equal(0, await db.DbContext.Orders.CountAsync());
            Assert.Equal(0, await db.DbContext.Payments.CountAsync());
        }

        [Fact]
        public async Task Checkout_RollsBackEarlierStockMutation_WhenLaterProductIsInsufficient()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var productA = await SeedProductAsync(db, user.Id, "SKU-ROLLBACK-A", stock: 5);
            var productB = await SeedProductAsync(db, user.Id, "SKU-ROLLBACK-B", stock: 1);
            await AddCartItemAsync(db, user.Id, productA, 1);
            await AddCartItemAsync(db, user.Id, productB, 5);

            var result = await db.Mediator.Send(CheckoutRequest());

            Assert.False(result.IsSuccess, result.Message ?? "expected checkout to fail");
            var stockA = await db.DbContext.Products.Where(p => p.Id == productA.Id).Select(p => p.StockQuantity).SingleAsync();
            var stockB = await db.DbContext.Products.Where(p => p.Id == productB.Id).Select(p => p.StockQuantity).SingleAsync();
            Assert.Equal(5, stockA);
            Assert.Equal(1, stockB);
            Assert.Equal(0, await db.DbContext.Orders.CountAsync());
            Assert.Equal(0, await db.DbContext.Payments.CountAsync());
        }

        [Fact]
        public async Task Checkout_Concurrent_OnlyOnePurchaseSucceeds()
        {
            using var db1 = new TestDb();

            var user = await db1.CreateUserAsync();
            db1.SetCurrentUser(user.Id);
            var product = await SeedProductAsync(db1, user.Id, "SKU-CONCURRENT", stock: 2);
            await AddCartItemAsync(db1, user.Id, product, 2);

            using var db2 = new TestDb(db1.ConnectionString);
            db2.SetCurrentUser(user.Id);

            var t1 = db1.Mediator.Send(CheckoutRequest());
            var t2 = db2.Mediator.Send(CheckoutRequest());

            var first = await t1;
            var second = await t2;

            var outcomes = new[] { first.IsSuccess, second.IsSuccess };
            Assert.Single(outcomes, o => o);
            Assert.Single(outcomes, o => !o);

            var stock = await db1.DbContext.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync();
            Assert.Equal(0, stock);
            Assert.Equal(1, await db1.DbContext.Orders.CountAsync());
        }

        [Fact]
        public async Task Checkout_WithCreditCard_IsRejectedWithoutTouchingStock()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var product = await SeedProductAsync(db, user.Id, "SKU-CARDREJECT", stock: 5);
            await AddCartItemAsync(db, user.Id, product, 2);

            var result = await db.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CreditCard
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("Credit card", result.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(0, await db.DbContext.Orders.CountAsync());
            Assert.Equal(0, await db.DbContext.Payments.CountAsync());
            Assert.Equal(5, await db.DbContext.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync());
        }

        [Fact]
        public async Task CreateCheckout_WithStripeUnavailable_RejectsCard()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var product = await SeedProductAsync(db, user.Id, "SKU-CARDUNAVAIL", stock: 5);
            await AddCartItemAsync(db, user.Id, product, 2);

            var result = await db.Mediator.Send(new CreateCheckoutCommand
            {
                ShippingAddress = ShippingAddress
            });

            Assert.False(result.IsSuccess);
            Assert.Contains("Credit card payments are unavailable", result.Message);
            Assert.Equal(0, await db.DbContext.Orders.CountAsync());
            Assert.Equal(0, await db.DbContext.Payments.CountAsync());
            Assert.Equal(5, await db.DbContext.Products.Where(p => p.Id == product.Id).Select(p => p.StockQuantity).SingleAsync());
        }
    }
}