using Application.Features.RecommendationFeatures.Queries;
using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.WishlistEntities;
using Microsoft.EntityFrameworkCore;
using Xunit;
using OrderEntity = Domain.Entities.OrderEntities.Order;

namespace Application.Tests.RecommendationFeatures
{
    public class GetRecommendedForUserTests
    {
        private static async Task<TestDb> SeedPersonalizedAsync()
        {
            var db = new TestDb();
            var user = await db.CreateUserAsync("rec@example.com");

            var catA = new Category { NameAR = "أ", NameEN = "CatA", DisplayOrder = 1, IsActive = true };
            var catB = new Category { NameAR = "ب", NameEN = "CatB", DisplayOrder = 2, IsActive = true };

            var signalPurchased = new Product { NameAR = "م1", NameEN = "Signal Purchased", SKU = "SKU-REC-P", Price = 100m, StockQuantity = 10, IsActive = true, Category = catA };
            var signalWish = new Product { NameAR = "م2", NameEN = "Signal Wish", SKU = "SKU-REC-W", Price = 100m, StockQuantity = 10, IsActive = true, Category = catB };
            var candidate = new Product { NameAR = "م3", NameEN = "Candidate", SKU = "SKU-REC-C1", Price = 100m, StockQuantity = 10, IsActive = true, Category = catA };
            db.DbContext.Products.AddRange(signalPurchased, signalWish, candidate);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.WishlistItems.Add(new WishlistItem { UserId = user.Id, ProductId = signalWish.Id });
            db.DbContext.Orders.Add(new OrderEntity
            {
                UserId = user.Id,
                OrderNumber = "ORD-REC-SIGNAL",
                Subtotal = 100m,
                Total = 100m,
                Status = OrderStatus.Paid,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                Items = new List<OrderItem>
                {
                    new() { ProductId = signalPurchased.Id, ProductName = "Signal Purchased", Quantity = 1, UnitPrice = 100m, TotalPrice = 100m }
                }
            });
            await db.DbContext.SaveChangesAsync();

            db.SetCurrentUser(user.Id);
            return db;
        }

        [Fact]
        public async Task Personalized_ExcludesSignals_AndRanksSharedCategoryCandidate()
        {
            using var db = await SeedPersonalizedAsync();
            var candidate = await db.DbContext.Products.SingleAsync(p => p.SKU == "SKU-REC-C1");

            var result = await db.Mediator.Send(new GetRecommendedForUserQuery { Count = 10 });

            Assert.True(result.IsSuccess, result.Message ?? "Recommendations failed");
            Assert.Equal("Personalized", result.Data!.Mode);
            Assert.Contains(result.Data.Products, p => p.Id == candidate.Id);
            Assert.DoesNotContain(result.Data.Products, p => p.SKU == "SKU-REC-P");
            Assert.DoesNotContain(result.Data.Products, p => p.SKU == "SKU-REC-W");
        }

        [Fact]
        public async Task Personalized_SplitsIntoNarrowScan_AndTopNDetailFetch()
        {
            using var db = await SeedPersonalizedAsync();

            await db.Mediator.Send(new GetRecommendedForUserQuery { Count = 10 });

            var productSelects = db.QueryCounter.Commands
                .Where(c => TestQueryCounter.IsSelect(c) &&
                            c.Contains("[catalog].[Product]", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // The wide all-products scan is a narrow projection: no review/image/variant
            // joins for scoring, and signal products are excluded in SQL.
            Assert.True(productSelects.Any(c =>
                    c.Contains("IsActive", StringComparison.OrdinalIgnoreCase) &&
                    c.Contains("NOT IN", StringComparison.OrdinalIgnoreCase) &&
                    !c.Contains("[dbo].[Review]", StringComparison.OrdinalIgnoreCase)),
                "expected narrow candidate scan:\n" + string.Join("\n---\n", productSelects));
            // Full product detail is loaded only for the top-ranked subset.
            Assert.True(productSelects.Any(c => c.Contains("[dbo].[Review]", StringComparison.OrdinalIgnoreCase)),
                "expected top-N detail fetch with reviews:\n" + string.Join("\n---\n", productSelects));
        }

        [Fact]
        public async Task ColdStart_FallsBackToPopular()
        {
            using var db = new TestDb();
            var user = await db.CreateUserAsync("rec-pop@example.com");
            var category = new Category { NameAR = "ف", NameEN = "Cat", DisplayOrder = 1, IsActive = true };
            var product = new Product { NameAR = "م", NameEN = "Bestseller", SKU = "SKU-REC-POP", Price = 100m, StockQuantity = 10, IsActive = true, Category = category };
            db.DbContext.Products.Add(product);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.Orders.Add(new OrderEntity
            {
                UserId = user.Id,
                OrderNumber = "ORD-REC-POP",
                Subtotal = 200m,
                Total = 200m,
                Status = OrderStatus.Paid,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                Items = new List<OrderItem>
                {
                    new() { ProductId = product.Id, ProductName = "Bestseller", Quantity = 2, UnitPrice = 100m, TotalPrice = 200m }
                }
            });
            await db.DbContext.SaveChangesAsync();
            db.SetCurrentUser(user.Id);

            var result = await db.Mediator.Send(new GetRecommendedForUserQuery { Count = 10 });

            Assert.True(result.IsSuccess, result.Message ?? "Recommendations failed");
            Assert.Equal("Popular", result.Data!.Mode);
            Assert.Single(result.Data.Products);
            Assert.Equal(product.Id, result.Data.Products[0].Id);
        }
    }
}