using Domain.Entities.WishlistEntities;
using Domain.Entities.CatalogEntities;

namespace Application.Features.WishlistFeatures.Commands
{
    public class AddToWishlistCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ProductId { get; set; } = string.Empty;

        public class AddToWishlistCommandHandler : IRequestHandler<AddToWishlistCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public AddToWishlistCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<string>> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                var existing = await _unitOfWork.Repository<WishlistItem>()
                    .FindByCondition(w => w.UserId == _currentUserService.UserId && w.ProductId == request.ProductId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existing != null)
                    return Result<string>.Falid(null, "Product is already in your wishlist.");

                var wishlistItem = new WishlistItem
                {
                    UserId = _currentUserService.UserId,
                    ProductId = request.ProductId,
                    AddedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<WishlistItem>().Create(wishlistItem);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, "Product added to wishlist.")
                    : Result<string>.Falid(null, "Failed to add to wishlist.");
            }
        }
    }

    public class RemoveFromWishlistCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ProductId { get; set; } = string.Empty;

        public class RemoveFromWishlistCommandHandler : IRequestHandler<RemoveFromWishlistCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public RemoveFromWishlistCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<string>> Handle(RemoveFromWishlistCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                var item = await _unitOfWork.Repository<WishlistItem>()
                    .FindByCondition(w => w.UserId == _currentUserService.UserId && w.ProductId == request.ProductId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (item == null)
                    return Result<string>.Falid(null, "Item not found in wishlist.");

                _unitOfWork.Repository<WishlistItem>().Delete(item);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, "Product removed from wishlist.")
                    : Result<string>.Falid(null, "Failed to remove from wishlist.");
            }
        }
    }
}
