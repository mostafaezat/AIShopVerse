using Domain.Entities.CatalogEntities;

namespace Application.Features.ProductFeatures.Queries
{
    public class GetNewArrivalsQuery : IRequest<Result<List<ProductListItemDto>>>
    {
        public int Count { get; set; } = 12;

        public class GetNewArrivalsQueryHandler : IRequestHandler<GetNewArrivalsQuery, Result<List<ProductListItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetNewArrivalsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<ProductListItemDto>>> Handle(GetNewArrivalsQuery request, CancellationToken cancellationToken)
            {
                var count = Math.Max(1, Math.Min(50, request.Count));

                var items = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Reviews)
                    .Include(p => p.Variants)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .ToListAsync(cancellationToken);

                return Result<List<ProductListItemDto>>.Success(
                    items.Select(ProductListMapper.ToListItem).ToList());
            }
        }
    }
}
