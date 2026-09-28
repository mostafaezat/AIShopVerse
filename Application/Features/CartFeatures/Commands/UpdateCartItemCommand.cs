using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Application.Features.CartFeatures.Queries;
using Application.Services;

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

                return Result<CartDto>.Success(await GetCartDtoAsync(cartItem.CartId, cancellationToken));
            }

            private async Task<CartDto> GetCartDtoAsync(string cartId, CancellationToken cancellationToken)
            {
                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.Id == cartId)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                var items = new List<CartItemDto>();
                decimal subtotal = 0;

                foreach (var item in cart!.Items)
                {
                    var product = await _unitOfWork.Repository<Product>()
                        .FindByCondition(p => p.Id == item.ProductId)
                        .Include(p => p.Images)
                        .Include(p => p.Variants)
                        .FirstOrDefaultAsync(cancellationToken);

                    string? variantLabel = null;
                    decimal currentPrice;

                    if (!string.IsNullOrEmpty(item.VariantId) && product != null)
                    {
                        var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
                        currentPrice = variant?.Price ?? item.UnitPrice;
                        variantLabel = ProductVariantLabels.For(variant);
                    }
                    else
                    {
                        currentPrice = product?.DiscountPrice ?? product?.Price ?? item.UnitPrice;
                    }

                    var itemTotal = currentPrice * item.Quantity;
                    subtotal += itemTotal;

                    items.Add(new CartItemDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        VariantId = item.VariantId,
                        VariantLabel = variantLabel,
                        ProductName = product?.NameEN ?? "",
                        ProductImageUrl = product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                        Quantity = item.Quantity,
                        UnitPrice = currentPrice,
                        TotalPrice = itemTotal
                    });
                }

                decimal tax = subtotal * 0.15m;
                decimal shipping = subtotal > 100 ? 0 : 10;
                decimal total = subtotal + tax + shipping;

                return new CartDto
                {
                    Id = cart.Id,
                    Items = items,
                    Subtotal = subtotal,
                    Tax = tax,
                    ShippingCost = shipping,
                    Total = total,
                    CouponCode = cart.CouponCode
                };
            }
        }
    }
}