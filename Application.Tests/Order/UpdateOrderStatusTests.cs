using Application.Features.OrderFeatures.Commands;
using Application.Features.PaymentFeatures.Commands;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Order
{
    public class UpdateOrderStatusTests
    {
        private static async Task<string> CheckoutAsync(TestDb db, string email, string sku, int quantity = 1)
        {
            var user = await db.CreateUserAsync(email);
            db.SetCurrentUser(user.Id);

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
                Price = 100m,
                StockQuantity = 10,
                IsActive = true,
                Category = category,
                Variants = new List<ProductVariant>
                {
                    new()
                    {
                        SKU = $"{sku}-M-RED",
                        Price = 100m,
                        StockQuantity = 10,
                        Size = "M",
                        Color = "Red",
                        IsActive = true
                    }
                }
            };
            db.DbContext.Products.Add(product);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.CartItems.Add(new CartItem
            {
                CartId = (await EnsureCartAsync(db, user.Id)).Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = 100m,
                VariantId = product.Variants.First().Id
            });
            await db.DbContext.SaveChangesAsync();

            var checkout = await db.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = "123 Test Street",
                PaymentMethod = PaymentMethod.CashOnDelivery
            });
            Assert.True(checkout.IsSuccess, checkout.Message ?? "checkout failed");

            return (await db.DbContext.Orders.SingleAsync()).Id;
        }

        private static async Task<Cart> EnsureCartAsync(TestDb db, string userId)
        {
            var cart = await db.DbContext.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                db.DbContext.Carts.Add(cart);
                await db.DbContext.SaveChangesAsync();
            }
            return cart;
        }

        private static UpdateOrderStatusCommand Update(string orderId, OrderStatus status)
            => new() { OrderId = orderId, NewStatus = status };

        [Fact]
        public async Task UpdateOrderStatus_WalksValidPath_PendingToDelivered()
        {
            using var db = new TestDb();
            var orderId = await CheckoutAsync(db, "walk@example.com", "SKU-WALK");

            Assert.True((await db.Mediator.Send(Update(orderId, OrderStatus.Processing))).IsSuccess);
            Assert.True((await db.Mediator.Send(Update(orderId, OrderStatus.Shipped))).IsSuccess);
            Assert.True((await db.Mediator.Send(Update(orderId, OrderStatus.Delivered))).IsSuccess);

            var order = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Delivered, order.Status);
        }

        [Fact]
        public async Task UpdateOrderStatus_Delivered_CompletesCodPayment()
        {
            using var db = new TestDb();
            var orderId = await CheckoutAsync(db, "cod@example.com", "SKU-COD");

            Assert.True((await db.Mediator.Send(Update(orderId, OrderStatus.Processing))).IsSuccess);
            Assert.True((await db.Mediator.Send(Update(orderId, OrderStatus.Shipped))).IsSuccess);
            Assert.True((await db.Mediator.Send(Update(orderId, OrderStatus.Delivered))).IsSuccess);

            var payment = await db.DbContext.Payments.SingleAsync();
            Assert.Equal(PaymentStatus.Completed, payment.Status);
            Assert.NotNull(payment.PaidAt);
            Assert.Equal("Collected on delivery.", payment.GatewayResponse);
        }

        [Fact]
        public async Task UpdateOrderStatus_InvalidTransition_Fails()
        {
            using var db = new TestDb();
            var orderId = await CheckoutAsync(db, "invalid@example.com", "SKU-INVALID");

            var result = await db.Mediator.Send(Update(orderId, OrderStatus.Delivered));

            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid status transition from Pending to Delivered.", result.Message);
            Assert.Equal(OrderStatus.Pending, (await db.DbContext.Orders.SingleAsync()).Status);
        }

        [Fact]
        public async Task UpdateOrderStatus_TerminalStateCannotMove_Fails()
        {
            using var db = new TestDb();
            var orderId = await CheckoutAsync(db, "terminal@example.com", "SKU-TERMINAL");
            foreach (var status in new[] { OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered })
                Assert.True((await db.Mediator.Send(Update(orderId, status))).IsSuccess);

            var result = await db.Mediator.Send(Update(orderId, OrderStatus.Cancelled));

            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid status transition from Delivered to Cancelled.", result.Message);
            Assert.Equal(OrderStatus.Delivered, (await db.DbContext.Orders.SingleAsync()).Status);
        }

        [Fact]
        public async Task UpdateOrderStatus_SameStatus_Fails()
        {
            using var db = new TestDb();
            var orderId = await CheckoutAsync(db, "same@example.com", "SKU-SAME");

            var result = await db.Mediator.Send(Update(orderId, OrderStatus.Pending));

            Assert.False(result.IsSuccess);
            Assert.Equal("Order is already Pending.", result.Message);
        }

        [Fact]
        public async Task RefundOrder_PaidOrder_RefundsPaymentAndOrder()
        {
            using var db = new TestDb();
            var orderId = await CheckoutAsync(db, "refund@example.com", "SKU-REFUND");
            Assert.True((await db.Mediator.Send(Update(orderId, OrderStatus.Paid))).IsSuccess);

            var result = await db.Mediator.Send(new RefundOrderCommand { OrderId = orderId });

            Assert.True(result.IsSuccess, result.Message ?? "refund failed");
            var order = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Refunded, order.Status);
            Assert.Equal(PaymentStatus.Refunded, (await db.DbContext.Payments.SingleAsync()).Status);
        }

        [Fact]
        public async Task RefundOrder_UnpaidOrder_Fails()
        {
            using var db = new TestDb();
            var orderId = await CheckoutAsync(db, "norefund@example.com", "SKU-NOREFUND");

            var result = await db.Mediator.Send(new RefundOrderCommand { OrderId = orderId });

            Assert.False(result.IsSuccess);
            Assert.Equal("Only paid orders can be refunded (current status: Pending).", result.Message);
        }
    }
}