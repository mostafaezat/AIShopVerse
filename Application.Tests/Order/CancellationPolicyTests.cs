using Application.Features.OrderFeatures.Commands;
using Application.Features.PaymentFeatures.Commands;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderEntity = Domain.Entities.OrderEntities.Order;

namespace Application.Tests.Order
{
    public class CancellationPolicyTests
    {
        private static async Task<(Product Product, ProductVariant Variant)> SeedProductAsync(TestDb db, string sku, int stock = 10)
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
            return (product, product.Variants.First());
        }

        private static async Task<OrderEntity> CheckoutCodAsync(TestDb db, string email, ProductVariant variant, int quantity)
        {
            var user = await db.CreateUserAsync(email);
            db.SetCurrentUser(user.Id);

            db.DbContext.CartItems.Add(new CartItem
            {
                CartId = (await EnsureCartAsync(db, user.Id)).Id,
                ProductId = variant.ProductId,
                Quantity = quantity,
                UnitPrice = variant.Price,
                VariantId = variant.Id
            });
            await db.DbContext.SaveChangesAsync();

            var checkout = await db.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = "123 Test Street",
                PaymentMethod = PaymentMethod.CashOnDelivery
            });
            Assert.True(checkout.IsSuccess, checkout.Message ?? "checkout failed");

            return await db.DbContext.Orders.SingleAsync();
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

        private static async Task<OrderEntity> SeedCardOrderAsync(TestDb db, string email, ProductVariant variant, bool stockDeducted, PaymentStatus paymentStatus, int quantity = 2)
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
                OrderNumber = $"ORD-SEED-{Guid.NewGuid():N}"[..16],
                Subtotal = variant.Price * quantity,
                Tax = 0,
                ShippingCost = 0,
                Total = variant.Price * quantity,
                Status = stockDeducted ? OrderStatus.Paid : OrderStatus.Pending,
                StockDeducted = stockDeducted,
                ShippingAddress = "123 Test Street",
                BillingAddress = "123 Test Street",
                CreatedAt = DateTime.UtcNow
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
                Method = nameof(PaymentMethod.CreditCard),
                Status = paymentStatus,
                TransactionId = "pi_seed_mock",
                GatewayResponse = "Seeded card payment."
            });
            await db.DbContext.SaveChangesAsync();
            return order;
        }

        [Fact]
        public async Task CancelOrder_UserCancelsPendingCodOrder_RestoresStock()
        {
            using var db = new TestDb();
            var (_, variant) = await SeedProductAsync(db, "SKU-CANCEL-COD");
            var order = await CheckoutCodAsync(db, "cancelcod@example.com", variant, 2);

            Assert.True(order.StockDeducted);
            Assert.Equal(8, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);

            var result = await db.Mediator.Send(new CancelOrderCommand { OrderId = order.Id });

            Assert.True(result.IsSuccess, result.Message ?? "cancel failed");
            var fresh = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Cancelled, fresh.Status);
            Assert.False(fresh.StockDeducted);
            Assert.Equal(10, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);
        }

        [Fact]
        public async Task CancelOrder_NeverDeductedCardOrder_StockUnchanged()
        {
            using var db = new TestDb();
            var (_, variant) = await SeedProductAsync(db, "SKU-NEVER-DEDUCTED");
            var order = await SeedCardOrderAsync(db, "neverdeducted@example.com", variant, stockDeducted: false, paymentStatus: PaymentStatus.Pending);

            var result = await db.Mediator.Send(new CancelOrderCommand { OrderId = order.Id });

            Assert.True(result.IsSuccess, result.Message ?? "cancel failed");
            var fresh = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Cancelled, fresh.Status);
            Assert.False(fresh.StockDeducted);
            Assert.Equal(10, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);
        }

        [Theory]
        [InlineData(OrderStatus.Processing)]
        [InlineData(OrderStatus.Shipped)]
        public async Task UpdateOrderStatus_AdminCancelsDeductedOrder_RestoresStock(OrderStatus before)
        {
            using var db = new TestDb();
            var (_, variant) = await SeedProductAsync(db, "SKU-ADMIN-CANCEL");
            var order = await CheckoutCodAsync(db, "admincancel@example.com", variant, 2);

            var path = new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered };
            for (var i = 0; i < path.Count && path[i] != before; i++)
                Assert.True((await db.Mediator.Send(new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = path[i] })).IsSuccess);

            Assert.True((await db.Mediator.Send(new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = OrderStatus.Cancelled })).IsSuccess);

            var fresh = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Cancelled, fresh.Status);
            Assert.False(fresh.StockDeducted);
            Assert.Equal(10, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);
        }

        [Fact]
        public async Task RefundOrder_PaidCardOrder_RestoresStockOnlyOnce()
        {
            using var db = new TestDb();
            var (_, variant) = await SeedProductAsync(db, "SKU-REFUND-STOCK");
            var order = await SeedCardOrderAsync(db, "refundstock@example.com", variant, stockDeducted: true, paymentStatus: PaymentStatus.Completed);

            Assert.Equal(8, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);

            var first = await db.Mediator.Send(new RefundOrderCommand { OrderId = order.Id });
            Assert.True(first.IsSuccess, first.Message ?? "refund failed");

            var fresh = await db.DbContext.Orders.SingleAsync();
            Assert.Equal(OrderStatus.Refunded, fresh.Status);
            Assert.False(fresh.StockDeducted);
            Assert.Equal(10, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);

            var second = await db.Mediator.Send(new RefundOrderCommand { OrderId = order.Id });
            Assert.False(second.IsSuccess);
            Assert.Equal(10, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);
        }

        [Fact]
        public async Task Delivered_IsTerminal_StockNeverRestoredAfterDelivery()
        {
            using var db = new TestDb();
            var (_, variant) = await SeedProductAsync(db, "SKU-DELIVERED");
            var order = await CheckoutCodAsync(db, "delivered@example.com", variant, 2);
            foreach (var status in new[] { OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered })
                Assert.True((await db.Mediator.Send(new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = status })).IsSuccess);

            var cancel = await db.Mediator.Send(new UpdateOrderStatusCommand { OrderId = order.Id, NewStatus = OrderStatus.Cancelled });
            Assert.False(cancel.IsSuccess);
            Assert.Equal(8, (await db.DbContext.ProductVariants.SingleAsync()).StockQuantity);
            Assert.True((await db.DbContext.Orders.SingleAsync()).StockDeducted);
        }
    }
}