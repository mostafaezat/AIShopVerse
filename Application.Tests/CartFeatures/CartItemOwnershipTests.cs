using Application.Features.CartFeatures.Commands;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.CartFeatures
{
    public class CartItemOwnershipTests
    {
        private static async Task<CartItem> SeedCartItemForAsync(TestDb db, string email, int quantity = 2)
        {
            var owner = await db.CreateUserAsync(email);

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
                NameEN = "Product IDOR",
                SKU = "SKU-IDOR",
                Price = 100m,
                StockQuantity = 10,
                IsActive = true,
                Category = category,
                Variants = new List<ProductVariant>
                {
                    new()
                    {
                        SKU = "SKU-IDOR-M-RED",
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

            var cart = new Cart { UserId = owner.Id };
            db.DbContext.Carts.Add(cart);
            await db.DbContext.SaveChangesAsync();

            var cartItem = new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = product.Variants.First().Id,
                Quantity = quantity,
                UnitPrice = 100m
            };
            db.DbContext.CartItems.Add(cartItem);
            await db.DbContext.SaveChangesAsync();

            db.SetCurrentUser(owner.Id);
            return cartItem;
        }

        [Fact]
        public async Task UpdateCartItem_AnotherUsersCartItem_Fails()
        {
            using var db = new TestDb();
            var cartItem = await SeedCartItemForAsync(db, "owner@example.com");
            var intruder = await db.CreateUserAsync("intruder@example.com");
            db.SetCurrentUser(intruder.Id);

            var result = await db.Mediator.Send(new UpdateCartItemCommand { CartItemId = cartItem.Id, Quantity = 9 });

            Assert.False(result.IsSuccess);
            Assert.Equal("You do not own this cart item.", result.Message);
            Assert.Equal(2, (await db.DbContext.CartItems.SingleAsync()).Quantity);
        }

        [Fact]
        public async Task UpdateCartItem_OwnersCartItem_Succeeds()
        {
            using var db = new TestDb();
            var cartItem = await SeedCartItemForAsync(db, "owner2@example.com");

            var result = await db.Mediator.Send(new UpdateCartItemCommand { CartItemId = cartItem.Id, Quantity = 5 });

            Assert.True(result.IsSuccess, result.Message ?? "update failed");
            var fresh = await db.DbContext.CartItems.SingleAsync();
            Assert.Equal(5, fresh.Quantity);
            Assert.Equal(5, result.Data!.Items.Single().Quantity);
        }

        [Fact]
        public async Task RemoveCartItem_AnotherUsersCartItem_Fails()
        {
            using var db = new TestDb();
            var cartItem = await SeedCartItemForAsync(db, "owner3@example.com");
            var intruder = await db.CreateUserAsync("intruder3@example.com");
            db.SetCurrentUser(intruder.Id);

            var result = await db.Mediator.Send(new RemoveCartItemCommand { CartItemId = cartItem.Id });

            Assert.False(result.IsSuccess);
            Assert.Equal("You do not own this cart item.", result.Message);
            Assert.Single(db.DbContext.CartItems);
        }
    }
}