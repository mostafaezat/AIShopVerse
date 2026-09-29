using Domain.Entities.CatalogEntities;

namespace Application.Features.ProductFeatures.Queries
{
    public class GetPromotionalProductsQuery : IRequest<Result<List<ProductListItemDto>>>
    {
        public int Count { get; set; } = 12;

        public class GetPromotionalProductsQueryHandler : IRequestHandler<GetPromotionalProductsQuery, Result<List<ProductListItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetPromotionalProductsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<ProductListItemDto>>> Handle(GetPromotionalProductsQuery request, CancellationToken cancellationToken)
            {
                var count = Math.Max(1, Math.Min(50, request.Count));

                var items = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive && p.DiscountPrice != null && p.DiscountPrice < p.Price)
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Reviews)
                    .Include(p => p.Variants)
                    .OrderByDescending(p => (p.Price - p.DiscountPrice!.Value) / p.Price)
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(count)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                return Result<List<ProductListItemDto>>.Success(
                    items.Select(ProductListMapper.ToListItem).ToList());
            }
        }
    }
}
