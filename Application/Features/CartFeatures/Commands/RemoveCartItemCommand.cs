using Domain.Entities.CartEntities;

namespace Application.Features.CartFeatures.Commands
{
    public class RemoveCartItemCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string CartItemId { get; set; } = string.Empty;

        public class RemoveCartItemCommandHandler : IRequestHandler<RemoveCartItemCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public RemoveCartItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<string>> Handle(RemoveCartItemCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                var cartItem = await _unitOfWork.Repository<CartItem>()
                    .FindByCondition(ci => ci.Id == request.CartItemId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cartItem == null)
                    return Result<string>.Falid(null, "Cart item not found.");

                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.Id == cartItem.CartId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cart == null || cart.UserId != _currentUserService.UserId)
                    return Result<string>.Falid(null, "You do not own this cart item.");

                _unitOfWork.Repository<CartItem>().Delete(cartItem);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, "Item removed from cart.")
                    : Result<string>.Falid(null, "Failed to remove item.");
            }
        }
    }
}
