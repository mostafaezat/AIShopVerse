using Domain.Entities.CatalogEntities;

namespace Application.Features.CategoryFeatures.Commands
{
    public class DeleteCategoryCommand : IRequest<Result<string>>
    {
        public string Id { get; set; } = string.Empty;

        public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public DeleteCategoryCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
            {
                var category = await _unitOfWork.Repository<Category>()
                    .FindByCondition(c => c.Id == request.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (category == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                _unitOfWork.Repository<Category>().Delete(category);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, ResourcesLocalizationKeys.DeleteSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.DeleteFailed);
            }
        }
    }
}
