using Domain.Entities.CatalogEntities;

namespace Application.Features.CategoryFeatures.Queries
{
    public class GetCategoriesWithCountsQuery : IRequest<Result<List<CategoryWithCountDto>>>
    {
        public string? BrandId { get; set; }

        public class GetCategoriesWithCountsQueryHandler : IRequestHandler<GetCategoriesWithCountsQuery, Result<List<CategoryWithCountDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetCategoriesWithCountsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<CategoryWithCountDto>>> Handle(GetCategoriesWithCountsQuery request, CancellationToken cancellationToken)
            {
                var allCategories = await _unitOfWork.Repository<Category>()
                    .FindAll()
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(cancellationToken);

                var productsQuery = _unitOfWork.Repository<Product>()
                    .FindAll()
                    .Where(p => p.IsActive);

                if (!string.IsNullOrEmpty(request.BrandId))
                    productsQuery = productsQuery.Where(p => p.BrandId == request.BrandId);

                var counts = await productsQuery
                    .GroupBy(p => p.CategoryId)
                    .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

                var result = allCategories.Select(c => new CategoryWithCountDto
                {
                    Id = c.Id,
                    NameAR = c.NameAR,
                    NameEN = c.NameEN,
                    ImageUrl = c.ImageUrl,
                    ParentId = c.ParentId,
                    DisplayOrder = c.DisplayOrder,
                    ProductCount = counts.GetValueOrDefault(c.Id, 0)
                }).ToList();

                return Result<List<CategoryWithCountDto>>.Success(result);
            }
        }
    }

    public class CategoryWithCountDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public int ProductCount { get; set; }
    }
}
