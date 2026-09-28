using Application.Features.OrderFeatures.Queries;
using Application.Services;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Domain.Entities.PromotionEntities;
using Infrastructure.Services.PaymentGateway;

namespace Application.Features.PaymentFeatures.Commands
{
    public class CreateCheckoutCommand : IRequest<Result<CreateCheckoutResponse>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ShippingAddress { get; set; } = string.Empty;

        public string? BillingAddress { get; set; }
        public string? CouponCode { get; set; }

        public class CreateCheckoutCommandHandler : IRequestHandler<CreateCheckoutCommand, Result<CreateCheckoutResponse>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;
            private readonly IPricingService _pricingService;
            private readonly IStripePaymentService _stripePayment;

            public CreateCheckoutCommandHandler(
                IUnitOfWork unitOfWork,
                ICurrentUserService currentUserService,
                IPricingService pricingService,
                IStripePaymentService stripePayment)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
                _pricingService = pricingService;
                _stripePayment = stripePayment;
            }

            public async Task<Result<CreateCheckoutResponse>> Handle(CreateCheckoutCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<CreateCheckoutResponse>.Falid(null, "User not authenticated.");

                // COD-only MVP: with Stripe not configured, card attempts are rejected clearly
                // instead of falling back to a mock "paid" state.
                if (!_stripePayment.IsEnabled)
                    return Result<CreateCheckoutResponse>.Falid(null, "Credit card payments are unavailable. Please use Cash on Delivery.");

                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.UserId == _currentUserService.UserId)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cart == null || !cart.Items.Any())
                    return Result<CreateCheckoutResponse>.Falid(null, "Cart is empty.");

                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var cartItem in cart.Items)
                {
                    var product = await _unitOfWork.Repository<Product>()
                        .FindByCondition(p => p.Id == cartItem.ProductId && p.IsActive)
                        .Include(p => p.Images)
                        .Include(p => p.Variants)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (product == null)
                        return Result<CreateCheckoutResponse>.Falid(null, $"Product {cartItem.ProductId} not found or inactive.");

                    ProductVariant? variant = null;
                    decimal unitPrice;
                    string? variantLabel = null;

                    if (!string.IsNullOrEmpty(cartItem.VariantId))
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Id == cartItem.VariantId && v.IsActive);
                        if (variant == null)
                            return Result<CreateCheckoutResponse>.Falid(null, "A selected variant is no longer available.");

                        unitPrice = variant.Price;
                        variantLabel = ProductVariantLabels.For(variant);

                        if (variant.StockQuantity < cartItem.Quantity)
                            return Result<CreateCheckoutResponse>.Falid(null, $"Insufficient stock for {product.NameEN}.");
                    }
                    else
                    {
                        if (product.Variants.Any(v => v.IsActive))
                            return Result<CreateCheckoutResponse>.Falid(null, $"A variant is required for {product.NameEN}.");

                        unitPrice = product.DiscountPrice ?? product.Price;

                        if (product.StockQuantity < cartItem.Quantity)
                            return Result<CreateCheckoutResponse>.Falid(null, $"Insufficient stock for {product.NameEN}.");
                    }

                    var itemTotal = unitPrice * cartItem.Quantity;
                    subtotal += itemTotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        VariantId = variant?.Id,
                        VariantLabel = variantLabel,
                        ProductName = product.NameEN,
                        ProductImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                        Quantity = cartItem.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = itemTotal
                    });
                }

                var couponResult = await _pricingService.ValidateAndApplyCouponAsync(request.CouponCode, subtotal, cancellationToken);
                if (!couponResult.IsValid)
                    return Result<CreateCheckoutResponse>.Falid(null, couponResult.Error ?? "Invalid coupon.");

                var pricing = _pricingService.ComputeTotals(subtotal, couponResult.DiscountAmount);

                var order = new Order
                {
                    UserId = _currentUserService.UserId,
                    OrderNumber = GenerateOrderNumber(),
                    Subtotal = pricing.Subtotal,
                    DiscountAmount = pricing.DiscountAmount,
                    Tax = pricing.Tax,
                    ShippingCost = pricing.ShippingCost,
                    Total = pricing.Total,
                    Status = OrderStatus.Pending,
                    ShippingAddress = request.ShippingAddress,
                    BillingAddress = request.BillingAddress ?? request.ShippingAddress,
                    CouponCode = string.IsNullOrWhiteSpace(request.CouponCode) ? null : request.CouponCode.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<Order>().Create(order);

                foreach (var item in orderItems)
                {
                    item.OrderId = order.Id;
                    _unitOfWork.Repository<OrderItem>().Create(item);
                }

                var stripeIntent = await _stripePayment.CreatePaymentIntentAsync(
                    ToCents(pricing.Total), order.Id, order.OrderNumber, cancellationToken);

                var pendingPayment = new Payment
                {
                    OrderId = order.Id,
                    Amount = pricing.Total,
                    Method = PaymentMethod.CreditCard.ToString(),
                    Status = PaymentStatus.Pending,
                    TransactionId = stripeIntent.PaymentIntentId,
                    GatewayResponse = "Awaiting payment confirmation."
                };
                _unitOfWork.Repository<Payment>().Create(pendingPayment);

                await _unitOfWork.CompleteAsync(cancellationToken);

                return Result<CreateCheckoutResponse>.Success(new CreateCheckoutResponse
                {
                    Mode = "card",
                    OrderId = order.Id,
                    PaymentIntentId = stripeIntent.PaymentIntentId,
                    ClientSecret = stripeIntent.ClientSecret
                }, "Payment intent created.");
            }

            private static long ToCents(decimal total) => (long)Math.Round(total * 100, MidpointRounding.AwayFromZero);

            private static string GenerateOrderNumber()
                => $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        }
    }

    public class CreateCheckoutResponse
    {
        public string Mode { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string? PaymentIntentId { get; set; }
        public string? ClientSecret { get; set; }
        public OrderDto? Order { get; set; }
    }
}
