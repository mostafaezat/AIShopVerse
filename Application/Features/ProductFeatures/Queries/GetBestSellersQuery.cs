using Domain.Entities.OrderEntities;
using Domain.Entities.CatalogEntities;

namespace Application.Features.ProductFeatures.Queries
{
    public class GetBestSellersQuery : IRequest<Result<List<ProductListItemDto>>>
    {
        public int Count { get; set; } = 12;

        public class GetBestSellersQueryHandler : IRequestHandler<GetBestSellersQuery, Result<List<ProductListItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetBestSellersQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<ProductListItemDto>>> Handle(GetBestSellersQuery request, CancellationToken cancellationToken)
            {
                var count = Math.Max(1, Math.Min(50, request.Count));

                var bestSellerUnits = await _unitOfWork.Repository<OrderItem>()
                    .FindAll()
                    .Join(
                        _unitOfWork.Repository<Order>().FindAll()
                            .Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded),
                        item => item.OrderId,
                        order => order.Id,
                        (item, order) => item)
                    .GroupBy(i => i.ProductId)
                    .Select(g => new { ProductId = g.Key, Units = g.Sum(i => i.Quantity) })
                    .OrderByDescending(x => x.Units)
                    .Take(count * 3)
                    .ToListAsync(cancellationToken);

                var bestSellerIds = bestSellerUnits.Select(x => x.ProductId).ToList();
                var unitsMap = bestSellerUnits.ToDictionary(x => x.ProductId, x => x.Units);

                if (bestSellerIds.Any())
                {
                    var products = await _unitOfWork.Repository<Product>()
                        .FindByCondition(p => p.IsActive && bestSellerIds.Contains(p.Id))
                        .Include(p => p.Category)
                        .Include(p => p.Brand)
                        .Include(p => p.Images)
                        .Include(p => p.Reviews)
                        .Include(p => p.Variants)
                        .AsNoTracking()
                        .ToListAsync(cancellationToken);

                    var ordered = products
                        .OrderByDescending(p => unitsMap.GetValueOrDefault(p.Id, 0))
                        .Take(count)
                        .Select(ProductListMapper.ToListItem)
                        .ToList();

                    if (ordered.Count >= count)
                        return Result<List<ProductListItemDto>>.Success(ordered);
                }

                var existingIds = bestSellerIds.ToHashSet();
                var fill = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive && !existingIds.Contains(p.Id))
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Reviews)
                    .Include(p => p.Variants)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var fallback = fill.Select(ProductListMapper.ToListItem).ToList();
                return Result<List<ProductListItemDto>>.Success(fallback);
            }
        }
    }
}
