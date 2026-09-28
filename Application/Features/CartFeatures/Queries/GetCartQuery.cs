using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Application.Services;

namespace Application.Features.CartFeatures.Queries
{
    public class GetCartQuery : IRequest<Result<CartDto>>
    {
        public string? CouponCode { get; set; }

        public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Result<CartDto>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;
            private readonly IPricingService _pricingService;

            public GetCartQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPricingService pricingService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
                _pricingService = pricingService;
            }

            public async Task<Result<CartDto>> Handle(GetCartQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<CartDto>.Falid(null, "User not authenticated.");

                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.UserId == _currentUserService.UserId)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cart == null)
                {
                    return Result<CartDto>.Success(new CartDto
                    {
                        Id = "",
                        Items = new List<CartItemDto>(),
                        Subtotal = 0,
                        Tax = 0,
                        ShippingCost = 0,
                        Total = 0
                    });
                }

                var items = new List<CartItemDto>();
                decimal subtotal = 0;

                foreach (var item in cart.Items)
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

                    item.UnitPrice = currentPrice;
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

                var couponResult = await _pricingService
                    .ValidateAndApplyCouponAsync(string.IsNullOrWhiteSpace(request.CouponCode) ? cart.CouponCode : request.CouponCode, subtotal, cancellationToken);

                decimal discount = couponResult.IsValid ? couponResult.DiscountAmount : 0;
                string? appliedCoupon = couponResult.IsValid
                    ? (string.IsNullOrWhiteSpace(request.CouponCode) ? cart.CouponCode : request.CouponCode.Trim())
                    : null;

                var pricing = _pricingService.ComputeTotals(subtotal, discount);

                if (cart.CouponCode != appliedCoupon)
                {
                    cart.CouponCode = appliedCoupon;
                    _unitOfWork.Repository<Cart>().Update(cart);
                    await _unitOfWork.CompleteAsync(cancellationToken);
                }

                return Result<CartDto>.Success(new CartDto
                {
                    Id = cart.Id,
                    Items = items,
                    Subtotal = pricing.Subtotal,
                    DiscountAmount = pricing.DiscountAmount,
                    Tax = pricing.Tax,
                    ShippingCost = pricing.ShippingCost,
                    Total = pricing.Total,
                    CouponCode = cart.CouponCode
                });
            }
        }
    }

    public class CartDto
    {
        public string Id { get; set; } = string.Empty;
        public List<CartItemDto> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public string? CouponCode { get; set; }
    }

    public class CartItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string? VariantId { get; set; }
        public string? VariantLabel { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
