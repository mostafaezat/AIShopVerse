using Domain.Entities.CatalogEntities;

namespace Application.Features.ProductFeatures.Queries
{
    public static class ProductListMapper
    {
        public static ProductListItemDto ToListItem(Product p)
        {
            return new ProductListItemDto
            {
                Id = p.Id,
                NameAR = p.NameAR,
                NameEN = p.NameEN,
                SKU = p.SKU,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive,
                HasVariants = p.Variants.Any(v => v.IsActive),
                CategoryName = p.Category?.NameEN,
                BrandName = p.Brand?.NameEN,
                PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
                AverageRating = p.Reviews.Where(r => r.IsApproved).Any() ? p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0,
                ReviewCount = p.Reviews.Count(r => r.IsApproved),
                CreatedAt = p.CreatedAt
            };
        }
    }
}
