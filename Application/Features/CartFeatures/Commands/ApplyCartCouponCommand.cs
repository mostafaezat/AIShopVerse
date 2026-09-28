using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Application.Features.CartFeatures.Queries;
using Application.Services;

namespace Application.Features.CartFeatures.Commands
{
    public class ApplyCartCouponCommand : IRequest<Result<CartDto>>
    {
        public string? CouponCode { get; set; }

        public class ApplyCartCouponCommandHandler : IRequestHandler<ApplyCartCouponCommand, Result<CartDto>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;
            private readonly IPricingService _pricingService;

            public ApplyCartCouponCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPricingService pricingService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
                _pricingService = pricingService;
            }

            public async Task<Result<CartDto>> Handle(ApplyCartCouponCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<CartDto>.Falid(null, "User not authenticated.");

                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.UserId == _currentUserService.UserId)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cart == null || cart.Items.Count == 0)
                    return Result<CartDto>.Falid(null, "Your cart is empty.");

                decimal subtotal = await ComputeSubtotalAsync(cart, cancellationToken);
                var couponResult = await _pricingService
                    .ValidateAndApplyCouponAsync(request.CouponCode, subtotal, cancellationToken);

                if (!couponResult.IsValid)
                    return Result<CartDto>.Falid(null, couponResult.Error!);

                cart.CouponCode = string.IsNullOrWhiteSpace(request.CouponCode) ? null : request.CouponCode!.Trim();
                _unitOfWork.Repository<Cart>().Update(cart);
                await _unitOfWork.CompleteAsync(cancellationToken);

                return Result<CartDto>.Success(await BuildCartDtoAsync(cart, cancellationToken));
            }

            private async Task<decimal> ComputeSubtotalAsync(Cart cart, CancellationToken cancellationToken)
            {
                decimal subtotal = 0;
                foreach (var item in cart.Items)
                {
                    var product = await _unitOfWork.Repository<Product>()
                        .FindByCondition(p => p.Id == item.ProductId)
                        .FirstOrDefaultAsync(cancellationToken);
                    var price = product?.DiscountPrice ?? product?.Price ?? item.UnitPrice;
                    subtotal += price * item.Quantity;
                }
                return subtotal;
            }

            private async Task<CartDto> BuildCartDtoAsync(Cart cart, CancellationToken cancellationToken)
            {
                var items = new List<CartItemDto>();
                decimal subtotal = 0;

                foreach (var item in cart.Items)
                {
                    var product = await _unitOfWork.Repository<Product>()
                        .FindByCondition(p => p.Id == item.ProductId)
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(cancellationToken);

                    var currentPrice = product?.DiscountPrice ?? product?.Price ?? item.UnitPrice;
                    item.UnitPrice = currentPrice;
                    var itemTotal = currentPrice * item.Quantity;
                    subtotal += itemTotal;

                    items.Add(new CartItemDto
                    {
                        Id = item.Id,
                        ProductId = item.ProductId,
                        ProductName = product?.NameEN ?? "",
                        ProductImageUrl = product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                        Quantity = item.Quantity,
                        UnitPrice = currentPrice,
                        TotalPrice = itemTotal
                    });
                }

                var couponResult = await _pricingService
                    .ValidateAndApplyCouponAsync(cart.CouponCode, subtotal, cancellationToken);
                decimal discount = couponResult.IsValid ? couponResult.DiscountAmount : 0;
                var pricing = _pricingService.ComputeTotals(subtotal, discount);

                return new CartDto
                {
                    Id = cart.Id,
                    Items = items,
                    Subtotal = pricing.Subtotal,
                    DiscountAmount = pricing.DiscountAmount,
                    Tax = pricing.Tax,
                    ShippingCost = pricing.ShippingCost,
                    Total = pricing.Total,
                    CouponCode = cart.CouponCode
                };
            }
        }
    }
}
