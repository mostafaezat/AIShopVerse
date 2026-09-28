using Domain.Entities.CatalogEntities;

namespace Application.Features.BrandFeatures.Commands
{
    public class AddBrandCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameAR { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameEN { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }
        public string? Description { get; set; }

        public class AddBrandCommandHandler : IRequestHandler<AddBrandCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public AddBrandCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(AddBrandCommand request, CancellationToken cancellationToken)
            {
                var brand = new Brand
                {
                    NameAR = request.NameAR,
                    NameEN = request.NameEN,
                    LogoUrl = request.LogoUrl,
                    Description = request.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<Brand>().Create(brand);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(brand.Id, ResourcesLocalizationKeys.AddSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.AddFailed);
            }
        }
    }
}
