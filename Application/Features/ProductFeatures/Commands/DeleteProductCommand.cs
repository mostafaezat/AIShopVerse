using Domain.Entities.CatalogEntities;

namespace Application.Features.ProductFeatures.Commands
{
    public class DeleteProductCommand : IRequest<Result<string>>
    {
        public string Id { get; set; } = string.Empty;

        public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public DeleteProductCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
            {
                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                _unitOfWork.Repository<Product>().Delete(product);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, ResourcesLocalizationKeys.DeleteSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.DeleteFailed);
            }
        }
    }
}
