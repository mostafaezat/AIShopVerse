using Domain.Entities.CatalogEntities;

namespace Application.Features.BrandFeatures.Commands
{
    public class UpdateBrandCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameAR { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameEN { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;

        public class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public UpdateBrandCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
            {
                var brand = await _unitOfWork.Repository<Brand>()
                    .FindByCondition(b => b.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (brand == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                brand.NameAR = request.NameAR;
                brand.NameEN = request.NameEN;
                brand.LogoUrl = request.LogoUrl;
                brand.Description = request.Description;
                brand.IsActive = request.IsActive;
                brand.LastModifiedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Brand>().Update(brand);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(brand.Id, ResourcesLocalizationKeys.UpdateSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.UpdateFailed);
            }
        }
    }
}
