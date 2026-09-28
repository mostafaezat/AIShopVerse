using Domain.Entities.CatalogEntities;

namespace Application.Features.BrandFeatures.Queries
{
    public class GetBrandsWithCountsQuery : IRequest<Result<List<BrandWithCountDto>>>
    {
        public string? CategoryId { get; set; }

        public class GetBrandsWithCountsQueryHandler : IRequestHandler<GetBrandsWithCountsQuery, Result<List<BrandWithCountDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetBrandsWithCountsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<BrandWithCountDto>>> Handle(GetBrandsWithCountsQuery request, CancellationToken cancellationToken)
            {
                var brandsQuery = _unitOfWork.Repository<Brand>()
                    .FindAll()
                    .Where(b => b.IsActive);

                var brands = await brandsQuery
                    .OrderBy(b => b.NameEN)
                    .ToListAsync(cancellationToken);

                var productsQuery = _unitOfWork.Repository<Product>()
                    .FindAll()
                    .Where(p => p.IsActive);

                if (!string.IsNullOrEmpty(request.CategoryId))
                    productsQuery = productsQuery.Where(p => p.CategoryId == request.CategoryId);

                var counts = await productsQuery
                    .Where(p => p.BrandId != null)
                    .GroupBy(p => p.BrandId!)
                    .Select(g => new { BrandId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.BrandId, x => x.Count, cancellationToken);

                var result = brands.Select(b => new BrandWithCountDto
                {
                    Id = b.Id,
                    NameAR = b.NameAR,
                    NameEN = b.NameEN,
                    LogoUrl = b.LogoUrl,
                    ProductCount = counts.GetValueOrDefault(b.Id, 0)
                }).ToList();

                return Result<List<BrandWithCountDto>>.Success(result);
            }
        }
    }

    public class BrandWithCountDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public int ProductCount { get; set; }
    }
}
