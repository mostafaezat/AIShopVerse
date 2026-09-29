using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Application.Features.CartFeatures.Queries;

namespace Application.Features.CartFeatures.Commands
{
    public class UpdateCartItemCommand : IRequest<Result<CartDto>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string CartItemId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.Invalid)]
        public int Quantity { get; set; }

        public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, Result<CartDto>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public UpdateCartItemCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<CartDto>> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<CartDto>.Falid(null, "User not authenticated.");

                var cartItem = await _unitOfWork.Repository<CartItem>()
                    .FindByCondition(ci => ci.Id == request.CartItemId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cartItem == null)
                    return Result<CartDto>.Falid(null, "Cart item not found.");

                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.Id == cartItem.CartId)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cart == null || cart.UserId != _currentUserService.UserId)
                    return Result<CartDto>.Falid(null, "You do not own this cart item.");

                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == cartItem.ProductId && p.IsActive)
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    return Result<CartDto>.Falid(null, "Product not found.");

                if (!string.IsNullOrEmpty(cartItem.VariantId))
                {
                    var variant = product.Variants.FirstOrDefault(v => v.Id == cartItem.VariantId && v.IsActive);
                    if (variant == null)
                        return Result<CartDto>.Falid(null, "Selected variant is not available.");

                    if (variant.StockQuantity < request.Quantity)
                        return Result<CartDto>.Falid(null, "Insufficient stock.");

                    cartItem.UnitPrice = variant.Price;
                }
                else
                {
                    if (product.Variants.Any(v => v.IsActive))
                        return Result<CartDto>.Falid(null, "A variant is required for this product.");

                    if (product.StockQuantity < request.Quantity)
                        return Result<CartDto>.Falid(null, "Insufficient stock.");

                    cartItem.UnitPrice = product.DiscountPrice ?? product.Price;
                }

                cartItem.Quantity = request.Quantity;

                _unitOfWork.Repository<CartItem>().Update(cartItem);
                await _unitOfWork.CompleteAsync(cancellationToken);

                var (items, subtotal) = await CartProjector.BuildItemsAsync(_unitOfWork, cart, cancellationToken);

                decimal tax = subtotal * 0.15m;
                decimal shipping = subtotal > 100 ? 0 : 10;
                decimal total = subtotal + tax + shipping;

                return Result<CartDto>.Success(new CartDto
                {
                    Id = cart.Id,
                    Items = items,
                    Subtotal = subtotal,
                    Tax = tax,
                    ShippingCost = shipping,
                    Total = total,
                    CouponCode = cart.CouponCode
                });
            }
        }
    }
}