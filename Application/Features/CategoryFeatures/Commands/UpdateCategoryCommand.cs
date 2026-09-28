using Domain.Entities.CatalogEntities;

namespace Application.Features.CategoryFeatures.Commands
{
    public class UpdateCategoryCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameAR { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameEN { get; set; } = string.Empty;

        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
            {
                var category = await _unitOfWork.Repository<Category>()
                    .FindByCondition(c => c.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (category == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                category.NameAR = request.NameAR;
                category.NameEN = request.NameEN;
                category.Description = request.Description;
                category.ImageUrl = request.ImageUrl;
                category.ParentId = request.ParentId;
                category.DisplayOrder = request.DisplayOrder;
                category.IsActive = request.IsActive;
                category.LastModifiedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Category>().Update(category);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(category.Id, ResourcesLocalizationKeys.UpdateSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.UpdateFailed);
            }
        }
    }
}
