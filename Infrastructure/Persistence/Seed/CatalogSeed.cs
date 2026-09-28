using System;
using System.IO;
using System.Text.Json;
using Domain.Entities.CatalogEntities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seed
{
    public static class CatalogSeed
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            if (await context.Categories.AnyAsync())
                return;

            var seedDir = Path.Combine(AppContext.BaseDirectory, "Persistence", "Seed", "SeedData", "Catalog");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var categoriesJson = await File.ReadAllTextAsync(Path.Combine(seedDir, "categories.json"));
            var categories = JsonSerializer.Deserialize<List<CategorySeedDto>>(categoriesJson, options);

            if (categories != null)
            {
                foreach (var cat in categories)
                {
                    context.Categories.Add(new Category
                    {
                        Id = cat.Id,
                        NameAR = cat.NameAR,
                        NameEN = cat.NameEN,
                        Description = cat.Description,
                        ImageUrl = cat.ImageUrl,
                        ParentId = cat.ParentId,
                        DisplayOrder = cat.DisplayOrder,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();
            }

            var brandsJson = await File.ReadAllTextAsync(Path.Combine(seedDir, "brands.json"));
            var brands = JsonSerializer.Deserialize<List<BrandSeedDto>>(brandsJson, options);

            if (brands != null)
            {
                foreach (var brand in brands)
                {
                    context.Brands.Add(new Brand
                    {
                        Id = brand.Id,
                        NameAR = brand.NameAR,
                        NameEN = brand.NameEN,
                        LogoUrl = brand.LogoUrl,
                        Description = brand.Description,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await context.SaveChangesAsync();
            }

            var productsJson = await File.ReadAllTextAsync(Path.Combine(seedDir, "products.json"));
            var products = JsonSerializer.Deserialize<List<ProductSeedDto>>(productsJson, options);

            if (products != null)
            {
                foreach (var prod in products)
                {
                    var product = new Product
                    {
                        Id = prod.Id,
                        NameAR = prod.NameAR,
                        NameEN = prod.NameEN,
                        SKU = prod.SKU,
                        Price = prod.Price,
                        DiscountPrice = prod.DiscountPrice,
                        StockQuantity = prod.StockQuantity,
                        CategoryId = prod.CategoryId,
                        BrandId = prod.BrandId,
                        DescriptionAR = prod.DescriptionAR,
                        DescriptionEN = prod.DescriptionEN,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Products.Add(product);

                    if (prod.ImageUrls != null)
                    {
                        int order = 1;
                        foreach (var url in prod.ImageUrls)
                        {
                            context.ProductImages.Add(new ProductImage
                            {
                                ImageUrl = url,
                                DisplayOrder = order,
                                IsPrimary = order == 1,
                                ProductId = product.Id
                            });
                            order++;
                        }
                    }
                }
                await context.SaveChangesAsync();
            }
        }

        private class CategorySeedDto
        {
            public string Id { get; set; } = string.Empty;
            public string NameEN { get; set; } = string.Empty;
            public string NameAR { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? ImageUrl { get; set; }
            public int DisplayOrder { get; set; }
            public string? ParentId { get; set; }
        }

        private class BrandSeedDto
        {
            public string Id { get; set; } = string.Empty;
            public string NameEN { get; set; } = string.Empty;
            public string NameAR { get; set; } = string.Empty;
            public string? LogoUrl { get; set; }
            public string? Description { get; set; }
        }

        private class ProductSeedDto
        {
            public string Id { get; set; } = string.Empty;
            public string NameEN { get; set; } = string.Empty;
            public string NameAR { get; set; } = string.Empty;
            public string SKU { get; set; } = string.Empty;
            public decimal Price { get; set; }
            public decimal? DiscountPrice { get; set; }
            public int StockQuantity { get; set; }
            public string CategoryId { get; set; } = string.Empty;
            public string? BrandId { get; set; }
            public string? DescriptionEN { get; set; }
            public string? DescriptionAR { get; set; }
            public List<string>? ImageUrls { get; set; }
        }
    }
}
