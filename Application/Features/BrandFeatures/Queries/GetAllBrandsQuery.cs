using Domain.Entities.CatalogEntities;

namespace Application.Features.BrandFeatures.Queries
{
    public class GetAllBrandsQuery : IRequest<Result<List<BrandDto>>>
    {
        public class GetAllBrandsQueryHandler : IRequestHandler<GetAllBrandsQuery, Result<List<BrandDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetAllBrandsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<BrandDto>>> Handle(GetAllBrandsQuery request, CancellationToken cancellationToken)
            {
                var brands = await _unitOfWork.Repository<Brand>()
                    .FindAll()
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.NameEN)
                    .Select(b => new BrandDto
                    {
                        Id = b.Id,
                        NameAR = b.NameAR,
                        NameEN = b.NameEN,
                        LogoUrl = b.LogoUrl,
                        Description = b.Description,
                        IsActive = b.IsActive
                    })
                    .ToListAsync(cancellationToken);

                return Result<List<BrandDto>>.Success(brands);
            }
        }
    }

    public class BrandDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
