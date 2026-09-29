using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Application.Features.CartFeatures.Queries;
using Application.Services;

namespace Application.Features.CartFeatures.Commands
{
    public class AddToCartCommand : IRequest<Result<CartDto>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ProductId { get; set; } = string.Empty;

        [MaxLength(36)]
        public string? VariantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = ErrorMessages.Invalid)]
        public int Quantity { get; set; } = 1;

        public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, Result<CartDto>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public AddToCartCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<CartDto>> Handle(AddToCartCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<CartDto>.Falid(null, "User not authenticated.");

                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == request.ProductId && p.IsActive)
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    return Result<CartDto>.Falid(null, "Product not found.");

                var activeVariants = product.Variants.Where(v => v.IsActive).ToList();

                decimal unitPrice;
                string? variantId = request.VariantId;
                string? variantLabel;

                if (activeVariants.Any())
                {
                    if (string.IsNullOrWhiteSpace(variantId))
                        return Result<CartDto>.Falid(null, "Please select a variant before adding to cart.");

                    var variant = activeVariants.FirstOrDefault(v => v.Id == variantId);
                    if (variant == null)
                        return Result<CartDto>.Falid(null, "Selected variant is not available.");

                    unitPrice = variant.Price;
                    variantLabel = ProductVariantLabels.For(variant);

                    if (variant.StockQuantity < request.Quantity)
                        return Result<CartDto>.Falid(null, "Insufficient stock.");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(variantId))
                        return Result<CartDto>.Falid(null, "Product has no variants.");

                    unitPrice = product.DiscountPrice ?? product.Price;
                    variantId = null;
                    variantLabel = null;

                    if (product.StockQuantity < request.Quantity)
                        return Result<CartDto>.Falid(null, "Insufficient stock.");
                }

                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.UserId == _currentUserService.UserId)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = _currentUserService.UserId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _unitOfWork.Repository<Cart>().Create(cart);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }

                var existingItem = cart.Items.FirstOrDefault(i =>
                    i.ProductId == request.ProductId && (i.VariantId ?? "") == (variantId ?? ""));
                if (existingItem != null)
                {
                    existingItem.Quantity += request.Quantity;
                    existingItem.UnitPrice = unitPrice;
                    existingItem.VariantId = variantId;
                }
                else
                {
                    var cartItem = new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = request.ProductId,
                        VariantId = variantId,
                        Quantity = request.Quantity,
                        UnitPrice = unitPrice
                    };
                    _unitOfWork.Repository<CartItem>().Create(cartItem);
                }

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