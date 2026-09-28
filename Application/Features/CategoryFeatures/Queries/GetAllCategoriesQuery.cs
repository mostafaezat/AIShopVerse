using Domain.Entities.CatalogEntities;

namespace Application.Features.CategoryFeatures.Queries
{
    public class GetAllCategoriesQuery : IRequest<Result<List<CategoryDto>>>
    {
        public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, Result<List<CategoryDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetAllCategoriesQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<CategoryDto>>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
            {
                var categories = await _unitOfWork.Repository<Category>()
                    .FindAll()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        NameAR = c.NameAR,
                        NameEN = c.NameEN,
                        Description = c.Description,
                        ImageUrl = c.ImageUrl,
                        ParentId = c.ParentId,
                        DisplayOrder = c.DisplayOrder,
                        IsActive = c.IsActive
                    })
                    .ToListAsync(cancellationToken);

                return Result<List<CategoryDto>>.Success(categories);
            }
        }
    }

    public class GetCategoryTreeQuery : IRequest<Result<List<CategoryTreeDto>>>
    {
        public class GetCategoryTreeQueryHandler : IRequestHandler<GetCategoryTreeQuery, Result<List<CategoryTreeDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetCategoryTreeQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<CategoryTreeDto>>> Handle(GetCategoryTreeQuery request, CancellationToken cancellationToken)
            {
                var all = await _unitOfWork.Repository<Category>()
                    .FindAll()
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(cancellationToken);

                var categories = all
                    .Where(c => c.ParentId == null)
                    .Select(c => new CategoryTreeDto
                    {
                        Id = c.Id,
                        NameAR = c.NameAR,
                        NameEN = c.NameEN,
                        ImageUrl = c.ImageUrl,
                        Children = all
                            .Where(ch => ch.ParentId == c.Id)
                            .Select(ch => new CategoryTreeDto
                            {
                                Id = ch.Id,
                                NameAR = ch.NameAR,
                                NameEN = ch.NameEN,
                                ImageUrl = ch.ImageUrl
                            })
                            .ToList()
                    })
                    .ToList();

                return Result<List<CategoryTreeDto>>.Success(categories);
            }
        }
    }

    public class CategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CategoryTreeDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public List<CategoryTreeDto> Children { get; set; } = new();
    }
}
