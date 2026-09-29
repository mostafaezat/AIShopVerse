using Application.Features.CartFeatures.Queries;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Xunit;

namespace Application.Tests.CartFeatures
{
    public class CartQueryPerformanceTests
    {
        private static async Task<TestDb> SeedMultiItemCartAsync()
        {
            var db = new TestDb();
            var user = await db.CreateUserAsync("cartperf@example.com");
            var category = new Category { NameAR = "فئة", NameEN = "Category", DisplayOrder = 1, IsActive = true };

            var products = new[]
            {
                new Product { NameAR = "أ", NameEN = "Alpha", SKU = "SKU-PRF-A", Price = 100m, StockQuantity = 10, IsActive = true, Category = category },
                new Product { NameAR = "ب", NameEN = "Beta", SKU = "SKU-PRF-B", Price = 200m, StockQuantity = 10, IsActive = true, Category = category },
                new Product { NameAR = "ج", NameEN = "Gamma", SKU = "SKU-PRF-C", Price = 300m, StockQuantity = 10, IsActive = true, Category = category }
            };
            db.DbContext.Products.AddRange(products);
            await db.DbContext.SaveChangesAsync();

            var cart = new Cart { UserId = user.Id };
            db.DbContext.Carts.Add(cart);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.CartItems.AddRange(
                new CartItem { CartId = cart.Id, ProductId = products[0].Id, Quantity = 2, UnitPrice = 100m },
                new CartItem { CartId = cart.Id, ProductId = products[1].Id, Quantity = 1, UnitPrice = 200m },
                new CartItem { CartId = cart.Id, ProductId = products[2].Id, Quantity = 3, UnitPrice = 300m });
            await db.DbContext.SaveChangesAsync();

            db.SetCurrentUser(user.Id);
            return db;
        }

        [Fact]
        public async Task GetCart_LoadsAllItemsWithSingleProductQuery()
        {
            using var db = await SeedMultiItemCartAsync();
            db.QueryCounter.Reset();

            var result = await db.Mediator.Send(new GetCartQuery());

            Assert.True(result.IsSuccess, result.Message ?? "GetCart failed");
            Assert.Equal(3, result.Data!.Items.Count);
            Assert.Equal(2 * 100m + 1 * 200m + 3 * 300m, result.Data.Subtotal);
            Assert.Equal("Gamma", result.Data.Items.Single(i => i.Quantity == 3).ProductName);
            Assert.Equal(1, db.QueryCounter.SelectCount("[Catalog].[Product]"));
        }

        [Fact]
        public async Task AddToCart_WithExistingItems_ReusesProjection()
        {
            using var db = await SeedMultiItemCartAsync();
            var cartItem = db.DbContext.CartItems.First();
            db.QueryCounter.Reset();

            var result = await db.Mediator.Send(new Application.Features.CartFeatures.Commands.AddToCartCommand
            {
                ProductId = cartItem.ProductId,
                Quantity = 5
            });

            Assert.True(result.IsSuccess, result.Message ?? "AddToCart failed");
            Assert.Equal(3, result.Data!.Items.Count);
        }
    }
}