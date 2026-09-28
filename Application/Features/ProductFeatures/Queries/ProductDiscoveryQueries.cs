using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;

namespace Application.Features.ProductFeatures.Queries
{
    public class GetRelatedProductsQuery : IRequest<Result<List<ProductListItemDto>>>
    {
        public string ProductId { get; set; } = string.Empty;
        public int Count { get; set; } = 8;

        public class GetRelatedProductsQueryHandler : IRequestHandler<GetRelatedProductsQuery, Result<List<ProductListItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetRelatedProductsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<ProductListItemDto>>> Handle(GetRelatedProductsQuery request, CancellationToken cancellationToken)
            {
                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == request.ProductId && p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    return Result<List<ProductListItemDto>>.Success(new List<ProductListItemDto>());

                var productParentId = product.Category.ParentId;
                var candidates = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive && p.Id != request.ProductId &&
                        (p.CategoryId == product.CategoryId ||
                         (productParentId != null && p.Category.ParentId == productParentId) ||
                         (product.BrandId != null && p.BrandId == product.BrandId)))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Attributes)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .ToListAsync(cancellationToken);

                var productAttrs = product.Attributes
                    .Where(a => !string.IsNullOrEmpty(a.Name) && !string.IsNullOrEmpty(a.Value))
                    .Select(a => $"{a.Name.Trim()}|{a.Value.Trim()}")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var related = candidates
                    .Select(p => new { Product = p, Score = ScoreRelated(p, productParentId, product, productAttrs) })
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenByDescending(x => x.Product.CreatedAt)
                    .Take(Math.Max(1, Math.Min(12, request.Count)))
                    .Select(x => ToListItem(x.Product))
                    .ToList();

                return Result<List<ProductListItemDto>>.Success(related);
            }

            private static int ScoreRelated(
                Product p,
                string? productParentId,
                Product product,
                HashSet<string> productAttrs)
            {
                int score = 0;

                if (p.CategoryId == product.CategoryId)
                    score += 8;
                else if (productParentId != null && p.Category.ParentId == productParentId)
                    score += 5;

                if (product.BrandId != null && p.BrandId == product.BrandId)
                    score += 4;

                foreach (var attr in p.Attributes)
                {
                    if (!string.IsNullOrEmpty(attr.Name) && !string.IsNullOrEmpty(attr.Value) &&
                        productAttrs.Contains($"{attr.Name.Trim()}|{attr.Value.Trim()}"))
                        score += 1;
                }

                return score;
            }

            private static ProductListItemDto ToListItem(Product p)
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
                    CategoryName = p.Category.NameEN,
                    BrandName = p.Brand != null ? p.Brand.NameEN : null,
                    PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
                    AverageRating = p.Reviews.Where(r => r.IsApproved).Any() ? p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0,
                    ReviewCount = p.Reviews.Count(r => r.IsApproved),
                    CreatedAt = p.CreatedAt
                };
            }
        }
    }

    public class GetFrequentlyBoughtTogetherQuery : IRequest<Result<List<ProductListItemDto>>>
    {
        public string ProductId { get; set; } = string.Empty;
        public int Count { get; set; } = 8;

        public class GetFrequentlyBoughtTogetherQueryHandler : IRequestHandler<GetFrequentlyBoughtTogetherQuery, Result<List<ProductListItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetFrequentlyBoughtTogetherQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<ProductListItemDto>>> Handle(GetFrequentlyBoughtTogetherQuery request, CancellationToken cancellationToken)
            {
                var coPurchased = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded &&
                        o.Items.Any(i => i.ProductId == request.ProductId))
                    .SelectMany(o => o.Items)
                    .Where(i => i.ProductId != request.ProductId)
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity) })
                    .OrderByDescending(x => x.Quantity)
                    .Take(Math.Max(1, Math.Min(12, request.Count)))
                    .ToListAsync(cancellationToken);

                if (coPurchased.Count == 0)
                    return Result<List<ProductListItemDto>>.Success(new List<ProductListItemDto>());

                var ids = coPurchased.Select(c => c.ProductId).ToList();
                var products = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive && ids.Contains(p.Id))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .ToListAsync(cancellationToken);

                var rank = coPurchased.Select((c, i) => new { c.ProductId, i }).ToDictionary(x => x.ProductId, x => x.i);

                var result = products
                    .OrderBy(p => rank.GetValueOrDefault(p.Id, int.MaxValue))
                    .Select(p => new ProductListItemDto
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
                        CategoryName = p.Category.NameEN,
                        BrandName = p.Brand != null ? p.Brand.NameEN : null,
                        PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
                        AverageRating = p.Reviews.Where(r => r.IsApproved).Any() ? p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0,
                        ReviewCount = p.Reviews.Count(r => r.IsApproved),
                        CreatedAt = p.CreatedAt
                    })
                    .ToList();

                return Result<List<ProductListItemDto>>.Success(result);
            }
        }
    }
}
