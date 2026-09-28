using Domain.Entities.WishlistEntities;

namespace Application.Features.WishlistFeatures.Queries
{
    public class IsInWishlistQuery : IRequest<Result<bool>>
    {
        public string ProductId { get; set; } = string.Empty;

        public class IsInWishlistQueryHandler : IRequestHandler<IsInWishlistQuery, Result<bool>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public IsInWishlistQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<bool>> Handle(IsInWishlistQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId) || string.IsNullOrWhiteSpace(request.ProductId))
                    return Result<bool>.Success(false);

                var exists = await _unitOfWork.Repository<WishlistItem>()
                    .FindByCondition(w => w.UserId == _currentUserService.UserId && w.ProductId == request.ProductId)
                    .AnyAsync(cancellationToken);

                return Result<bool>.Success(exists);
            }
        }
    }
}
