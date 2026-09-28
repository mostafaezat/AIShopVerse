using Domain.Entities.CatalogEntities;

namespace Application.Features.CategoryFeatures.Commands
{
    public class AddCategoryCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameAR { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameEN { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ParentId { get; set; }
        public int DisplayOrder { get; set; }

        public class AddCategoryCommandHandler : IRequestHandler<AddCategoryCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public AddCategoryCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(AddCategoryCommand request, CancellationToken cancellationToken)
            {
                var category = new Category
                {
                    NameAR = request.NameAR,
                    NameEN = request.NameEN,
                    Description = request.Description,
                    ImageUrl = request.ImageUrl,
                    ParentId = request.ParentId,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<Category>().Create(category);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(category.Id, ResourcesLocalizationKeys.AddSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.AddFailed);
            }
        }
    }
}
