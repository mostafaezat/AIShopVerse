using Application.Features.CartFeatures.Commands;
using Application.Features.OrderFeatures.Commands;
using Application.Features.ProductFeatures.Commands;
using Application.Features.ProductFeatures.Queries;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.NotificationEntities;
using Domain.Entities.PaymentEntities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Application.Tests.Catalog
{
    public class VariantPurchasingTests
    {
        private const string ShippingAddress = "123 Test Street";

        private static async Task<Category> SeedCategoryAsync(TestDb db)
        {
            var category = new Category
            {
                NameAR = "فئة",
                NameEN = "Category",
                DisplayOrder = 1,
                IsActive = true
            };
            db.DbContext.Categories.Add(category);
            await db.DbContext.SaveChangesAsync();
            return category;
        }

        private static async Task<(Product Product, ProductVariant Red, ProductVariant Blue)> SeedVariantProductAsync(
            TestDb db,
            string productSku = "SKU-VARIANT",
            decimal productPrice = 100m,
            int productStock = 20)
        {
            var category = await SeedCategoryAsync(db);

            var product = new Product
            {
                NameAR = "منتج بمقاسات",
                NameEN = $"Product {productSku}",
                SKU = productSku,
                Price = productPrice,
                StockQuantity = productStock,
                IsActive = true,
                CategoryId = category.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.DbContext.Products.Add(product);
            await db.DbContext.SaveChangesAsync();

            var red = new ProductVariant
            {
                ProductId = product.Id,
                SKU = $"{productSku}-M-RED",
                Price = 20m,
                StockQuantity = 5,
                Size = "M",
                Color = "Red",
                IsActive = true
            };
            var blue = new ProductVariant
            {
                ProductId = product.Id,
                SKU = $"{productSku}-L-BLUE",
                Price = 25m,
                StockQuantity = 4,
                Size = "L",
                Color = "Blue",
                IsActive = true
            };
            db.DbContext.ProductVariants.AddRange(red, blue);
            await db.DbContext.SaveChangesAsync();

            return (product, red, blue);
        }

        [Fact]
        public async Task AddToCart_OnVariantProduct_WithoutSelection_IsRejected()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, _, _) = await SeedVariantProductAsync(db);

            var result = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                Quantity = 1
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Please select a variant before adding to cart.", result.Message);
            Assert.Equal(0, await db.DbContext.CartItems.CountAsync());
        }

        [Fact]
        public async Task AddToCart_OnVariantProduct_WithUnknownVariant_IsRejected()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, _, _) = await SeedVariantProductAsync(db);

            var result = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = Guid.NewGuid().ToString(),
                Quantity = 1
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Selected variant is not available.", result.Message);
            Assert.Equal(0, await db.DbContext.CartItems.CountAsync());
        }

        [Fact]
        public async Task AddToCart_OnPlainProduct_WithVariantId_IsRejected()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var category = await SeedCategoryAsync(db);
            var product = new Product
            {
                NameAR = "منتج",
                NameEN = "Plain Product",
                SKU = "SKU-PLAIN",
                Price = 10m,
                StockQuantity = 5,
                IsActive = true,
                CategoryId = category.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.DbContext.Products.Add(product);
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = Guid.NewGuid().ToString(),
                Quantity = 1
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Product has no variants.", result.Message);
        }

        [Fact]
        public async Task AddToCart_Variant_UsesVariantPriceAndStockAndMergesByProductVariant()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, red, blue) = await SeedVariantProductAsync(db);

            var first = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 2
            });
            Assert.True(first.IsSuccess, first.Message ?? "add to cart failed");

            var second = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 3
            });
            Assert.True(second.IsSuccess, second.Message ?? "add to cart failed");

            var third = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = red.Id,
                Quantity = 1
            });
            Assert.True(third.IsSuccess, third.Message ?? "add to cart failed");

            var items = third.Data!.Items;
            var blueItem = items.Single(i => i.VariantId == blue.Id);
            Assert.Equal(5, blueItem.Quantity);
            Assert.Equal(25m, blueItem.UnitPrice);
            Assert.Equal(125m, blueItem.TotalPrice);
            Assert.Contains("L", blueItem.VariantLabel);
            Assert.Contains("Blue", blueItem.VariantLabel);

            var redItem = items.Single(i => i.VariantId == red.Id);
            Assert.Equal(1, redItem.Quantity);
            Assert.Equal(20m, redItem.UnitPrice);

            Assert.Equal(2, await db.DbContext.CartItems.Where(ci => ci.ProductId == product.Id).CountAsync());
        }

        [Fact]
        public async Task AddToCart_Variant_WithInsufficientStock_IsRejected()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, _, blue) = await SeedVariantProductAsync(db);

            var result = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 5
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Insufficient stock.", result.Message);

            var ok = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 4
            });
            Assert.True(ok.IsSuccess, ok.Message ?? "add to cart failed");
        }

        [Fact]
        public async Task UpdateCartItem_Variant_WithExcessQuantity_IsRejected()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, _, blue) = await SeedVariantProductAsync(db);

            var added = await db.Mediator.Send(new AddToCartCommand
            {
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 1
            });
            var item = added.Data!.Items.First();

            var result = await db.Mediator.Send(new UpdateCartItemCommand
            {
                CartItemId = item.Id,
                Quantity = 5
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Insufficient stock.", result.Message);
            Assert.Equal(1, (await db.DbContext.CartItems.SingleAsync()).Quantity);
        }

        [Fact]
        public async Task Checkout_Variant_DecrementsVariantStockAndSnapshotsOrderItem()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, red, blue) = await SeedVariantProductAsync(db);

            var cart = new Cart { UserId = user.Id };
            db.DbContext.Carts.Add(cart);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 2,
                UnitPrice = blue.Price
            });
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CashOnDelivery
            });
            Assert.True(result.IsSuccess, result.Message ?? "checkout failed");

            var freshVariant = await db.DbContext.ProductVariants
                .Where(v => v.Id == blue.Id)
                .SingleAsync();
            Assert.Equal(2, freshVariant.StockQuantity);

            var untouchedVariant = await db.DbContext.ProductVariants
                .Where(v => v.Id == red.Id)
                .SingleAsync();
            Assert.Equal(5, untouchedVariant.StockQuantity);

            var freshProduct = await db.DbContext.Products
                .Where(p => p.Id == product.Id)
                .SingleAsync();
            Assert.Equal(20, freshProduct.StockQuantity);

            var order = await db.DbContext.Orders
                .Include(o => o.Items)
                .SingleAsync();
            var orderItem = order.Items.Single();
            Assert.Equal(blue.Id, orderItem.VariantId);
            Assert.Equal(25m, orderItem.UnitPrice);
            Assert.Equal(2, orderItem.Quantity);
            Assert.Contains("L", orderItem.VariantLabel);
            Assert.Contains("Blue", orderItem.VariantLabel);

            Assert.Equal(0, await db.DbContext.CartItems.CountAsync());
        }

        [Fact]
        public async Task Checkout_Variant_WithInsufficientStock_FailsWithoutMutation()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, _, blue) = await SeedVariantProductAsync(db);

            var cart = new Cart { UserId = user.Id };
            db.DbContext.Carts.Add(cart);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 5,
                UnitPrice = blue.Price
            });
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CashOnDelivery
            });

            Assert.False(result.IsSuccess);
            Assert.Equal(4, (await db.DbContext.ProductVariants.Where(v => v.Id == blue.Id).SingleAsync()).StockQuantity);
            Assert.Equal(0, await db.DbContext.Orders.CountAsync());
            Assert.Equal(0, await db.DbContext.Payments.CountAsync());
        }

        [Fact]
        public async Task Checkout_Variant_Concurrent_OnlyOnePurchaseSucceeds()
        {
            using var db1 = new TestDb();

            var user = await db1.CreateUserAsync();
            db1.SetCurrentUser(user.Id);
            var (product, _, blue) = await SeedVariantProductAsync(db1);
            blue.StockQuantity = 2;
            await db1.DbContext.SaveChangesAsync();

            var cart = new Cart { UserId = user.Id };
            db1.DbContext.Carts.Add(cart);
            await db1.DbContext.SaveChangesAsync();

            db1.DbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 2,
                UnitPrice = blue.Price
            });
            await db1.DbContext.SaveChangesAsync();

            using var db2 = new TestDb(db1.ConnectionString);
            db2.SetCurrentUser(user.Id);

            var t1 = db1.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CashOnDelivery
            });
            var t2 = db2.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CashOnDelivery
            });

            var first = await t1;
            var second = await t2;

            var outcomes = new[] { first.IsSuccess, second.IsSuccess };
            Assert.Single(outcomes, o => o);
            Assert.Single(outcomes, o => !o);

            var stock = await db1.DbContext.ProductVariants
                .Where(v => v.Id == blue.Id)
                .Select(v => v.StockQuantity)
                .SingleAsync();
            Assert.Equal(0, stock);
            Assert.Equal(1, await db1.DbContext.Orders.CountAsync());
        }

        [Fact]
        public async Task Checkout_Variant_LowStockAlert_UsesVariantStock()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (product, _, blue) = await SeedVariantProductAsync(db);
            product.LowStockThreshold = 3;
            blue.StockQuantity = 5;
            await db.DbContext.SaveChangesAsync();

            var cart = new Cart { UserId = user.Id };
            db.DbContext.Carts.Add(cart);
            await db.DbContext.SaveChangesAsync();

            db.DbContext.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = blue.Id,
                Quantity = 3,
                UnitPrice = blue.Price
            });
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(new CheckoutCommand
            {
                ShippingAddress = ShippingAddress,
                PaymentMethod = PaymentMethod.CashOnDelivery
            });
            Assert.True(result.IsSuccess, result.Message ?? "checkout failed");

            var stock = await db.DbContext.ProductVariants
                .Where(v => v.Id == blue.Id)
                .Select(v => v.StockQuantity)
                .SingleAsync();
            Assert.Equal(2, stock);
            Assert.Equal(20, (await db.DbContext.Products.Where(p => p.Id == product.Id).SingleAsync()).StockQuantity);
            Assert.True(await db.DbContext.Notifications.AnyAsync(n => n.Type == NotificationType.Sale),
                "expected a low-stock notification");
        }

        [Fact]
        public async Task AddProduct_WithDuplicateVariantSku_IsRejected()
        {
            using var db = new TestDb();
            var category = await SeedCategoryAsync(db);

            var result = await db.Mediator.Send(new AddProductCommand
            {
                NameAR = "منتج",
                NameEN = "Duplicated SKU Variant",
                SKU = "SKU-DUPSKU-PRODUCT",
                Price = 50m,
                StockQuantity = 10,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "DUPSKU", Price = 20m, StockQuantity = 3, Size = "M", Color = "Red" },
                    new() { SKU = "dupsku", Price = 25m, StockQuantity = 3, Size = "L", Color = "Blue" }
                }
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Variant SKU 'dupsku' is duplicated.", result.Message, ignoreCase: true);
            Assert.Equal(0, await db.DbContext.Products.CountAsync());
        }

        [Fact]
        public async Task AddProduct_WithDuplicateSizeColor_IsRejected()
        {
            using var db = new TestDb();
            var category = await SeedCategoryAsync(db);

            var result = await db.Mediator.Send(new AddProductCommand
            {
                NameAR = "منتج",
                NameEN = "Duplicated Option Variant",
                SKU = "SKU-DUPOPT-PRODUCT",
                Price = 50m,
                StockQuantity = 10,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "SKU-A", Price = 20m, StockQuantity = 3, Size = "M", Color = "Red" },
                    new() { SKU = "SKU-B", Price = 25m, StockQuantity = 3, Size = "M", Color = "RED" }
                }
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("Variant size/color combination is duplicated.", result.Message);
            Assert.Equal(0, await db.DbContext.Products.CountAsync());
        }

        [Fact]
        public async Task AddProduct_WithVariantsAndCollisionOnAnotherProduct_IsRejected()
        {
            using var db = new TestDb();
            var category = await SeedCategoryAsync(db);

            var baseProduct = new Product
            {
                NameAR = "منتج",
                NameEN = "Base Product",
                SKU = "SKU-BASE",
                Price = 40m,
                StockQuantity = 5,
                IsActive = true,
                CategoryId = category.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.DbContext.Products.Add(baseProduct);
            await db.DbContext.SaveChangesAsync();
            db.DbContext.ProductVariants.Add(new ProductVariant
            {
                ProductId = baseProduct.Id,
                SKU = "SHARED-SKU",
                Price = 10m,
                StockQuantity = 1,
                Size = "S",
                Color = "Black"
            });
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(new AddProductCommand
            {
                NameAR = "منتج",
                NameEN = "Collision Product",
                SKU = "SKU-COLLIDE",
                Price = 50m,
                StockQuantity = 10,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "SHARED-SKU", Price = 20m, StockQuantity = 3, Size = "M", Color = "Red" }
                }
            });

            Assert.False(result.IsSuccess);
            Assert.Equal("A variant SKU already exists.", result.Message);
        }

        [Fact]
        public async Task AddProduct_WithVariants_CreatesActiveVariants()
        {
            using var db = new TestDb();
            var category = await SeedCategoryAsync(db);

            var result = await db.Mediator.Send(new AddProductCommand
            {
                NameAR = "منتج",
                NameEN = "Variant Product",
                SKU = "SKU-VARPRODUCT",
                Price = 50m,
                StockQuantity = 10,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "SKU-VAR-1", Price = 20m, StockQuantity = 3, Size = "M", Color = "Red" },
                    new() { SKU = "SKU-VAR-2", Price = 25m, StockQuantity = 4, Size = "L", Color = "Blue" }
                }
            });

            Assert.True(result.IsSuccess, result.Message ?? "add product failed");

            var variants = await db.DbContext.ProductVariants.ToListAsync();
            Assert.Equal(2, variants.Count);
            Assert.All(variants, v => Assert.True(v.IsActive));
            Assert.Contains(variants, v => v.SKU == "SKU-VAR-1" && v.Size == "M" && v.Color == "Red" && v.Price == 20m);
            Assert.Contains(variants, v => v.SKU == "SKU-VAR-2" && v.Size == "L" && v.Color == "Blue" && v.StockQuantity == 4);
        }

        [Fact]
        public async Task UpdateProduct_RemovesVariant_DeactivatesItAndUpdatesKept()
        {
            using var db = new TestDb();
            var category = await SeedCategoryAsync(db);

            var created = await db.Mediator.Send(new AddProductCommand
            {
                NameAR = "منتج",
                NameEN = "Variant Product",
                SKU = "SKU-UPDVARIANT",
                Price = 50m,
                StockQuantity = 10,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "SKU-UPD-1", Price = 20m, StockQuantity = 3, Size = "M", Color = "Red" },
                    new() { SKU = "SKU-UPD-2", Price = 25m, StockQuantity = 4, Size = "L", Color = "Blue" }
                }
            });
            Assert.True(created.IsSuccess, created.Message ?? "add product failed");

            var updated = await db.Mediator.Send(new UpdateProductCommand
            {
                Id = created.Data!,
                NameAR = "منتج",
                NameEN = "Variant Product",
                SKU = "SKU-UPDVARIANT",
                Price = 50m,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "SKU-UPD-2", Price = 30m, StockQuantity = 7, Size = "L", Color = "Blue" },
                    new() { SKU = "SKU-UPD-3", Price = 35m, StockQuantity = 2, Size = "XL", Color = "Green" }
                }
            });
            Assert.True(updated.IsSuccess, updated.Message ?? "update product failed");

            var variants = await db.DbContext.ProductVariants.Include(v => v.Product).ToListAsync();
            var red = variants.Single(v => v.SKU == "SKU-UPD-1");
            Assert.False(red.IsActive);

            var blue = variants.Single(v => v.SKU == "SKU-UPD-2");
            Assert.True(blue.IsActive);
            Assert.Equal(30m, blue.Price);
            Assert.Equal(7, blue.StockQuantity);

            var green = variants.Single(v => v.SKU == "SKU-UPD-3");
            Assert.True(green.IsActive);
            Assert.Equal("XL", green.Size);
            Assert.Equal("Green", green.Color);
        }

        [Fact]
        public async Task UpdateProduct_WithNewVariantCollidingWithKeptOption_IsRejected()
        {
            using var db = new TestDb();
            var category = await SeedCategoryAsync(db);

            var created = await db.Mediator.Send(new AddProductCommand
            {
                NameAR = "منتج",
                NameEN = "Variant Product",
                SKU = "SKU-COLLIDEOPT",
                Price = 50m,
                StockQuantity = 10,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "SKU-CO-1", Price = 20m, StockQuantity = 3, Size = "M", Color = "Red" }
                }
            });
            Assert.True(created.IsSuccess, created.Message ?? "add product failed");

            var updated = await db.Mediator.Send(new UpdateProductCommand
            {
                Id = created.Data!,
                NameAR = "منتج",
                NameEN = "Variant Product",
                SKU = "SKU-COLLIDEOPT",
                Price = 50m,
                CategoryId = category.Id,
                Variants = new List<ProductVariantDto>
                {
                    new() { SKU = "SKU-CO-1", Price = 20m, StockQuantity = 3, Size = "M", Color = "Red" },
                    new() { SKU = "SKU-CO-2", Price = 25m, StockQuantity = 3, Size = "M", Color = "red" }
                }
            });

            Assert.False(updated.IsSuccess);
            Assert.Equal("Variant size/color combination is duplicated.", updated.Message);
        }

        [Fact]
        public async Task GetNewArrivals_HasVariants_FlagsVariantProductsOnly()
        {
            using var db = new TestDb();

            var user = await db.CreateUserAsync();
            db.SetCurrentUser(user.Id);
            var (variantProduct, _, _) = await SeedVariantProductAsync(db, "SKU-NEWARRIVAL");
            var category = await SeedCategoryAsync(db);
            var plainProduct = new Product
            {
                NameAR = "منتج",
                NameEN = "Plain Product",
                SKU = "SKU-NEWARRIVAL-PLAIN",
                Price = 10m,
                StockQuantity = 5,
                IsActive = true,
                CategoryId = category.Id,
                CreatedAt = DateTime.UtcNow
            };
            db.DbContext.Products.Add(plainProduct);
            await db.DbContext.SaveChangesAsync();

            var result = await db.Mediator.Send(new GetNewArrivalsQuery { Count = 12 });
            Assert.True(result.IsSuccess, result.Message ?? "query failed");

            var variantItem = result.Data!.Single(p => p.Id == variantProduct.Id);
            Assert.True(variantItem.HasVariants);

            var plainItem = result.Data!.Single(p => p.Id == plainProduct.Id);
            Assert.False(plainItem.HasVariants);
        }
    }
}