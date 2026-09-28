using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Infrastructure.Services.PaymentGateway;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using OrderEntity = Domain.Entities.OrderEntities.Order;

namespace Application.Tests.Order
{
    public class ExpiredOrderProcessorTests
    {
        private static async Task<ProductVariant> SeedVariantAsync(TestDb db, string sku, int stock = 10)
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
                Price = 100m,
                StockQuantity = stock,
                IsActive = true,
                Category = category,
                Variants = new List<ProductVariant>
                {
                    new()
                    {
                        SKU = $"{sku}-M-RED",
                        Price = 100m,
                        StockQuantity = stock,
                        Size = "M",
                        Color = "Red",
                        IsActive = true
                    }
                }
            };
            db.DbContext.Products.Add(product);
            await db.DbContext.SaveChangesAsync();
            return product.Variants.First();
        }

        private static async Task<OrderEntity> SeedOrderAsync(
            TestDb db, string email, ProductVariant variant, PaymentMethod method, DateTime createdAt, bool stockDeducted, int quantity = 2)
        {
            var user = await db.CreateUserAsync(email);
            db.SetCurrentUser(user.Id);

            if (stockDeducted)
            {
                variant.StockQuantity -= quantity;
                db.DbContext.ProductVariants.Update(variant);
            }

            var order = new OrderEntity
            {
                UserId = user.Id,
                OrderNumber = $"ORD-EXP-{Guid.NewGuid():N}"[..16],
                Subtotal = variant.Price * quantity,
                Tax = 0,
                ShippingCost = 0,
                Total = variant.Price * quantity,
                Status = OrderStatus.Pending,
                StockDeducted = stockDeducted,
                ShippingAddress = "123 Test Street",
                BillingAddress = "123 Test Street",
                CreatedAt = createdAt
            };
            order.Items.Add(new OrderItem
            {
                ProductId = variant.ProductId,
                VariantId = variant.Id,
                VariantLabel = "M / Red",
                ProductName = "Seeded",
                Quantity = quantity,
                UnitPrice = variant.Price,
                TotalPrice = variant.Price * quantity
            });
            db.DbContext.Orders.Add(order);
            db.DbContext.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.Total,
                Method = method.ToString(),
                Status = PaymentStatus.Pending,
                TransactionId = method == PaymentMethod.CreditCard ? "pi_seed_mock" : null,
                GatewayResponse = "Seeded."
            });
            await db.DbContext.SaveChangesAsync();
            return order;
        }

        [Fact]
        public async Task ProcessAsync_ExpiresOldUnpaidCardOrder_AndIsIdempotent()
        {
            using var db = new TestDb();
            var variant = await SeedVariantAsync(db, "SKU-EXP-CARD");
            var order = await SeedOrderAsync(db, "expcard@example.com", variant, PaymentMethod.CreditCard, DateTime.UtcNow.AddHours(-2), stockDeducted: false);
            var processor = db.Services.GetRequiredService<IExpiredOrderProcessor>();

            var count = await processor.ProcessAsync();

            Assert.Equal(1, count);
            var fresh = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Cancelled, fresh.Status);
            var payment = await db.DbContext.Payments.SingleAsync();
            Assert.Equal(PaymentStatus.Failed, payment.Status);
            Assert.Equal("Order expired before payment was completed.", payment.GatewayResponse);
            Assert.Single(db.DbContext.Notifications.Where(n => n.OrderId == order.Id));
            Assert.Equal(10, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);

            var second = await processor.ProcessAsync();
            Assert.Equal(0, second);
            Assert.Equal(OrderStatus.Cancelled, (await db.DbContext.Orders.SingleAsync()).Status);
        }

        [Fact]
        public async Task ProcessAsync_LeavesRecentCardOrderPending()
        {
            using var db = new TestDb();
            var variant = await SeedVariantAsync(db, "SKU-EXP-RECENT");
            await SeedOrderAsync(db, "recent@example.com", variant, PaymentMethod.CreditCard, DateTime.UtcNow, stockDeducted: false);
            var processor = db.Services.GetRequiredService<IExpiredOrderProcessor>();

            var count = await processor.ProcessAsync();

            Assert.Equal(0, count);
            Assert.Equal(OrderStatus.Pending, (await db.DbContext.Orders.SingleAsync()).Status);
        }

        [Fact]
        public async Task ProcessAsync_IgnoresCodOrders_AndTheirDeductedStock()
        {
            using var db = new TestDb();
            var variant = await SeedVariantAsync(db, "SKU-EXP-COD");
            await SeedOrderAsync(db, "expcod@example.com", variant, PaymentMethod.CashOnDelivery, DateTime.UtcNow.AddHours(-2), stockDeducted: true);
            var processor = db.Services.GetRequiredService<IExpiredOrderProcessor>();

            var count = await processor.ProcessAsync();

            Assert.Equal(0, count);
            var fresh = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Pending, fresh.Status);
            Assert.True(fresh.StockDeducted);
            Assert.Equal(8, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);
        }
    }
}