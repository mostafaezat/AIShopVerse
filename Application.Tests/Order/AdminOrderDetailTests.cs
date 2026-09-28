using Application.Features.OrderFeatures.Commands;
using Application.Features.OrderFeatures.Queries;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.PaymentEntities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Order
{
    public class AdminOrderDetailTests
    {
        private const string ShippingAddress = "123 Test Street";

        private static async Task<Product> SeedProductAsync(TestDb db, string sku, decimal price = 100m, int stock = 10)
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
                IsActive = true,
                Category = category,
                Variants = new List<ProductVariant>
                {
                    new()
                    {
                        SKU = $"{sku}-M-RED",
                        Price = price,
                        StockQuantity = stock,
                        Size = "M",
                        Color = "Red",
                        IsActive = true
                    }
                }
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

            var variant = product.Variants.First();
            db.DbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = quantity,
                UnitPrice = variant.Price,
                VariantId = variant.Id
            });

            await db.DbContext.SaveChangesAsync();
        }

        private static CheckoutCommand CheckoutRequest()
            => new()
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CashOnDelivery
            };

        [Fact]
        public async Task GetAdminOrderById_ReturnsAnotherUsersOrder_WithCustomerAndVariantItems()
        {
            using var db = new TestDb();

            var owner = await db.CreateUserAsync("owner@example.com");
            db.SetCurrentUser(owner.Id);
            var product = await SeedProductAsync(db, "SKU-ADMIN-DETAIL");
            await AddCartItemAsync(db, owner.Id, product, 2);

            var checkout = await db.Mediator.Send(CheckoutRequest());
            Assert.True(checkout.IsSuccess, checkout.Message ?? "checkout failed");

            var other = await db.CreateUserAsync("other@example.com");
            db.SetCurrentUser(other.Id);

            var order = await db.DbContext.Orders.SingleAsync();
            var result = await db.Mediator.Send(new GetAdminOrderByIdQuery { OrderId = order.Id });

            Assert.True(result.IsSuccess, result.Message ?? "admin query failed");
            var data = result.Data;
            Assert.NotNull(data);
            Assert.Equal(order.OrderNumber, data.OrderNumber);
            Assert.NotNull(data.Customer);
            Assert.Equal("owner@example.com", data.Customer.Email);
            Assert.Equal("Test User", data.Customer.FullName);
            Assert.Equal("01000000001", data.Customer.PhoneNumber);

            var item = Assert.Single(data.Items);
            Assert.Equal(product.NameEN, item.ProductName);
            Assert.Equal("M / Red", item.VariantLabel);
            Assert.Equal(product.Variants.First().Id, item.VariantId);
            Assert.Equal(2, item.Quantity);
        }

        [Fact]
        public async Task GetAdminOrderById_UnknownOrder_Fails()
        {
            using var db = new TestDb();

            var result = await db.Mediator.Send(new GetAdminOrderByIdQuery
            {
                OrderId = Guid.NewGuid().ToString()
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Order not found.", result.Message);
        }

        [Fact]
        public async Task GetAdminOrderById_MapsPaymentFields()
        {
            using var db = new TestDb();

            var owner = await db.CreateUserAsync("payowner@example.com");
            db.SetCurrentUser(owner.Id);
            var product = await SeedProductAsync(db, "SKU-ADMIN-PAY", price: 50m);
            await AddCartItemAsync(db, owner.Id, product, 3);

            var checkout = await db.Mediator.Send(CheckoutRequest());
            Assert.True(checkout.IsSuccess, checkout.Message ?? "checkout failed");

            var order = await db.DbContext.Orders.SingleAsync();
            var result = await db.Mediator.Send(new GetAdminOrderByIdQuery { OrderId = order.Id });

            Assert.True(result.IsSuccess, result.Message ?? "admin query failed");
            var data = result.Data;
            Assert.NotNull(data);
            Assert.NotNull(data.Payment);
            Assert.Equal(order.Total, data.Payment.Amount);
            Assert.Equal("CashOnDelivery", data.Payment.Method);
            Assert.Equal("Pending", data.Payment.Status);
            Assert.Null(data.Payment.TransactionId);
            Assert.Null(data.Payment.PaidAt);
        }
    }
}