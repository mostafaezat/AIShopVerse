using Application.Features.DashboardFeatures.Queries;
using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Xunit;
using OrderEntity = Domain.Entities.OrderEntities.Order;

namespace Application.Tests.DashboardFeatures
{
    public class DashboardQueryPerformanceTests
    {
        private static async Task<TestDb> SeedOrdersAsync()
        {
            var db = new TestDb();
            var user = await db.CreateUserAsync("dashboard@example.com");
            var category = new Category { NameAR = "فئة", NameEN = "Category", DisplayOrder = 1, IsActive = true };
            var product = new Product
            {
                NameAR = "منتج",
                NameEN = "Dash Product",
                SKU = "SKU-DASH",
                Price = 100m,
                StockQuantity = 10,
                IsActive = true,
                Category = category
            };
            db.DbContext.Products.Add(product);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.Orders.AddRange(
                new OrderEntity
                {
                    UserId = user.Id,
                    OrderNumber = "ORD-RECENT",
                    Subtotal = 250m,
                    DiscountAmount = 0,
                    Tax = 0,
                    ShippingCost = 0,
                    Total = 250m,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    Items = new List<OrderItem>
                    {
                        new() { ProductId = product.Id, ProductName = product.NameEN, Quantity = 1, UnitPrice = 250m, TotalPrice = 250m }
                    }
                },
                new OrderEntity
                {
                    UserId = user.Id,
                    OrderNumber = "ORD-OLD",
                    Subtotal = 99m,
                    DiscountAmount = 0,
                    Tax = 0,
                    ShippingCost = 0,
                    Total = 99m,
                    Status = OrderStatus.Pending,
                    CreatedAt = DateTime.UtcNow.AddDays(-40),
                    Items = new List<OrderItem>
                    {
                        new() { ProductId = product.Id, ProductName = product.NameEN, Quantity = 1, UnitPrice = 99m, TotalPrice = 99m }
                    }
                });
            await db.DbContext.SaveChangesAsync();

            return db;
        }

        [Fact]
        public async Task DaysFilter_IsPushedDownToSql()
        {
            using var db = await SeedOrdersAsync();
            db.QueryCounter.Reset();

            var result = await db.Mediator.Send(new GetDashboardAnalyticsQuery { Days = 14, TopProductCount = 3 });

            Assert.True(result.IsSuccess, result.Message ?? "Dashboard failed");
            Assert.Equal(1, result.Data!.TotalOrders);
            Assert.Equal(250m, result.Data.TotalRevenue);

            var ordersSelect = db.QueryCounter.Commands.First(c =>
                TestQueryCounter.IsSelect(c) &&
                c.Contains("[sales].[Order]", StringComparison.OrdinalIgnoreCase) &&
                c.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase));
            Assert.True(ordersSelect.Contains("[CreatedAt]", StringComparison.OrdinalIgnoreCase) &&
                        ordersSelect.Contains(">=", StringComparison.OrdinalIgnoreCase), ordersSelect);
        }

        [Fact]
        public async Task NoDays_FiltersNothing_ReturnsAllOrders()
        {
            using var db = await SeedOrdersAsync();
            db.QueryCounter.Reset();

            var result = await db.Mediator.Send(new GetDashboardAnalyticsQuery { TopProductCount = 3 });

            Assert.True(result.IsSuccess, result.Message ?? "Dashboard failed");
            Assert.Equal(2, result.Data!.TotalOrders);
            Assert.Equal(349m, result.Data.TotalRevenue);
            Assert.Equal(2, result.Data.RecentOrders.Count);

            var ordersSelect = db.QueryCounter.Commands.First(c =>
                TestQueryCounter.IsSelect(c) &&
                c.Contains("[sales].[Order]", StringComparison.OrdinalIgnoreCase) &&
                c.Contains("ORDER BY", StringComparison.OrdinalIgnoreCase));
            Assert.True(!ordersSelect.Contains(">=", StringComparison.OrdinalIgnoreCase), ordersSelect);
        }
    }
}