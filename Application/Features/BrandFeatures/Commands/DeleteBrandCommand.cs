using Domain.Entities.CatalogEntities;

namespace Application.Features.BrandFeatures.Commands
{
    public class DeleteBrandCommand : IRequest<Result<string>>
    {
        public string Id { get; set; } = string.Empty;

        public class DeleteBrandCommandHandler : IRequestHandler<DeleteBrandCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public DeleteBrandCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
            {
                var brand = await _unitOfWork.Repository<Brand>()
                    .FindByCondition(b => b.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (brand == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                _unitOfWork.Repository<Brand>().Delete(brand);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, ResourcesLocalizationKeys.DeleteSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.DeleteFailed);
            }
        }
    }
}
