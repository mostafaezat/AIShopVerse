using Domain.Entities.OrderEntities;
using Domain.Entities.WishlistEntities;
using Domain.Entities.CatalogEntities;
using Application.Features.ProductFeatures.Queries;

namespace Application.Features.RecommendationFeatures.Queries
{
    public class GetRecommendedForUserQuery : IRequest<Result<RecommendedForUserDto>>
    {
        public int Count { get; set; } = 12;

        public class GetRecommendedForUserQueryHandler : IRequestHandler<GetRecommendedForUserQuery, Result<RecommendedForUserDto>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public GetRecommendedForUserQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<RecommendedForUserDto>> Handle(GetRecommendedForUserQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<RecommendedForUserDto>.Falid(null, "User not authenticated.");

                var count = Math.Max(1, Math.Min(50, request.Count));
                var userId = _currentUserService.UserId;

                var purchasedProductIds = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.UserId == userId &&
                        o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded)
                    .SelectMany(o => o.Items)
                    .Select(i => i.ProductId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var wishlistProductIds = await _unitOfWork.Repository<WishlistItem>()
                    .FindByCondition(w => w.UserId == userId)
                    .Select(w => w.ProductId)
                    .ToListAsync(cancellationToken);

                var signalIds = purchasedProductIds
                    .Concat(wishlistProductIds)
                    .Distinct()
                    .ToList();

                // Cold start: not enough history -> popular fallback.
                if (signalIds.Count < 2)
                {
                    var popular = await GetPopularProductsAsync(count, cancellationToken);
                    return Result<RecommendedForUserDto>.Success(new RecommendedForUserDto
                    {
                        Mode = "Popular",
                        Products = popular
                    });
                }

                var recommended = await GetPersonalizedAsync(userId, signalIds, purchasedProductIds, wishlistProductIds, count, cancellationToken);
                return Result<RecommendedForUserDto>.Success(new RecommendedForUserDto
                {
                    Mode = "Personalized",
                    Products = recommended
                });
            }

            private async Task<List<ProductListItemDto>> GetPersonalizedAsync(
                string userId,
                List<string> signalIds,
                List<string> purchasedProductIds,
                List<string> wishlistProductIds,
                int count,
                CancellationToken cancellationToken)
            {
                var signalProducts = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => signalIds.Contains(p.Id))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Attributes)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var categoryWeight = new Dictionary<string, int>();
                var parentWeight = new Dictionary<string, int>();
                var brandWeight = new Dictionary<string, int>();
                var attributeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var sp in signalProducts)
                {
                    if (!string.IsNullOrEmpty(sp.CategoryId))
                        categoryWeight[sp.CategoryId] = categoryWeight.GetValueOrDefault(sp.CategoryId) + 1;

                    if (!string.IsNullOrEmpty(sp.Category?.ParentId))
                        parentWeight[sp.Category.ParentId] = parentWeight.GetValueOrDefault(sp.Category.ParentId) + 1;

                    if (!string.IsNullOrEmpty(sp.BrandId))
                        brandWeight[sp.BrandId] = brandWeight.GetValueOrDefault(sp.BrandId) + 1;

                    foreach (var attr in sp.Attributes)
                    {
                        if (!string.IsNullOrEmpty(attr.Name) && !string.IsNullOrEmpty(attr.Value))
                            attributeSet.Add($"{attr.Name}|{attr.Value}");
                    }
                }

                var excludeIds = signalIds.ToHashSet();
                var bestSellerUnits = await GetBestSellerUnitsAsync(cancellationToken);

                // Narrow scan: only the columns needed for scoring, before loading
                // full product detail for the top-ranked subset.
                var candidateRows = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive && !excludeIds.Contains(p.Id))
                    .AsNoTracking()
                    .Select(p => new
                    {
                        p.Id,
                        p.CategoryId,
                        CategoryParentId = p.Category != null ? p.Category.ParentId : null,
                        p.BrandId,
                        p.StockQuantity,
                        p.CreatedAt,
                        p.Attributes
                    })
                    .ToListAsync(cancellationToken);

                var scored = new List<(double Score, string ProductId, DateTime CreatedAt)>();
                foreach (var c in candidateRows)
                {
                    double score = 0;

                    if (categoryWeight.TryGetValue(c.CategoryId, out var cw))
                        score += 5 * cw;

                    if (c.CategoryParentId != null && parentWeight.TryGetValue(c.CategoryParentId, out var pw))
                        score += 3 * pw;

                    if (!string.IsNullOrEmpty(c.BrandId) && brandWeight.TryGetValue(c.BrandId, out var bw))
                        score += 3 * bw;

                    foreach (var attr in c.Attributes)
                    {
                        if (!string.IsNullOrEmpty(attr.Name) && !string.IsNullOrEmpty(attr.Value)
                            && attributeSet.Contains($"{attr.Name}|{attr.Value}"))
                            score += 1;
                    }

                    if (c.StockQuantity > 0)
                        score += 0.5;

                    if (bestSellerUnits.TryGetValue(c.Id, out var units))
                        score += Math.Min(2, units / 10.0);

                    scored.Add((score, c.Id, c.CreatedAt));
                }

                var topIds = scored
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.CreatedAt)
                    .Take(count)
                    .Select(x => x.ProductId)
                    .ToList();

                var detailProducts = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => topIds.Contains(p.Id))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var detailMap = detailProducts.ToDictionary(p => p.Id);

                var top = topIds
                    .Where(detailMap.ContainsKey)
                    .Select(id => ToListItemDto(detailMap[id]))
                    .ToList();

                return top;
            }

            private async Task<List<ProductListItemDto>> GetPopularProductsAsync(int count, CancellationToken cancellationToken)
            {
                var bestSellerUnits = await GetBestSellerUnitsAsync(cancellationToken);

                var bestSellerIds = bestSellerUnits
                    .OrderByDescending(kv => kv.Value)
                    .Take(count * 3)
                    .Select(kv => kv.Key)
                    .ToList();

                var products = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive && bestSellerIds.Contains(p.Id))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var ordered = products
                    .OrderByDescending(p => bestSellerUnits.GetValueOrDefault(p.Id))
                    .Take(count)
                    .Select(ToListItemDto)
                    .ToList();

                if (ordered.Count >= count)
                    return ordered;

                var haveIds = ordered.Select(p => p.Id).ToHashSet();
                var fill = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive && !haveIds.Contains(p.Id))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count - ordered.Count)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                ordered.AddRange(fill.Select(ToListItemDto));
                return ordered;
            }

            private async Task<Dictionary<string, int>> GetBestSellerUnitsAsync(CancellationToken cancellationToken)
            {
                var rows = await _unitOfWork.Repository<OrderItem>()
                    .FindAll()
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Units = g.Sum(i => i.Quantity) })
                    .ToListAsync(cancellationToken);

                return rows.ToDictionary(r => r.ProductId, r => r.Units);
            }

            private static ProductListItemDto ToListItemDto(Product p)
            {
                var approved = p.Reviews.Where(r => r.IsApproved).ToList();
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
                    AverageRating = approved.Any() ? approved.Average(r => r.Rating) : 0,
                    ReviewCount = approved.Count,
                    CreatedAt = p.CreatedAt
                };
            }
        }
    }

    public class RecommendedForUserDto
    {
        public string Mode { get; set; } = "Personalized";
        public List<ProductListItemDto> Products { get; set; } = new();
    }
}
