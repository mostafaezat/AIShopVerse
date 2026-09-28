using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Domain.Entities.PromotionEntities;
using Application.Features.OrderFeatures.Queries;
using Application.Features.InventoryFeatures;
using Application.Services;

namespace Application.Features.OrderFeatures.Commands
{
    public class CheckoutCommand : IRequest<Result<OrderDto>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ShippingAddress { get; set; } = string.Empty;

        public string? BillingAddress { get; set; }
        public string? CouponCode { get; set; }
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public class CheckoutCommandHandler : IRequestHandler<CheckoutCommand, Result<OrderDto>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;
            private readonly IPricingService _pricingService;
            private readonly ILowStockAlerter _lowStockAlerter;

            public CheckoutCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IPricingService pricingService, ILowStockAlerter lowStockAlerter)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
                _pricingService = pricingService;
                _lowStockAlerter = lowStockAlerter;
            }

            public async Task<Result<OrderDto>> Handle(CheckoutCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<OrderDto>.Falid(null, "User not authenticated.");

                if (request.PaymentMethod == PaymentMethod.CreditCard)
                    return Result<OrderDto>.Falid(null, "Credit card payments are handled by the card checkout flow. Please use the payment gateway.");

                var cart = await _unitOfWork.Repository<Cart>()
                    .FindByCondition(c => c.UserId == _currentUserService.UserId)
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (cart == null || !cart.Items.Any())
                    return Result<OrderDto>.Falid(null, "Cart is empty.");

                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();
                var lineContexts = new List<(OrderItem Item, Product Product, ProductVariant? Variant)>();

                foreach (var cartItem in cart.Items)
                {
                    var product = await _unitOfWork.Repository<Product>()
                        .FindByCondition(p => p.Id == cartItem.ProductId && p.IsActive)
                        .Include(p => p.Images)
                        .Include(p => p.Variants)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (product == null)
                        return Result<OrderDto>.Falid(null, $"Product {cartItem.ProductId} not found or inactive.");

                    ProductVariant? variant = null;
                    decimal unitPrice;
                    string? variantLabel = null;

                    if (!string.IsNullOrEmpty(cartItem.VariantId))
                    {
                        variant = product.Variants.FirstOrDefault(v => v.Id == cartItem.VariantId && v.IsActive);
                        if (variant == null)
                            return Result<OrderDto>.Falid(null, "A selected variant is no longer available.");

                        unitPrice = variant.Price;
                        variantLabel = ProductVariantLabels.For(variant);
                    }
                    else
                    {
                        if (product.Variants.Any(v => v.IsActive))
                            return Result<OrderDto>.Falid(null, $"A variant is required for {product.NameEN}.");

                        unitPrice = product.DiscountPrice ?? product.Price;
                    }

                    var itemTotal = unitPrice * cartItem.Quantity;
                    subtotal += itemTotal;

                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        VariantId = variant?.Id,
                        VariantLabel = variantLabel,
                        ProductName = product.NameEN,
                        ProductImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                        Quantity = cartItem.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = itemTotal
                    };

                    orderItems.Add(orderItem);
                    lineContexts.Add((orderItem, product, variant));
                }

                // Validate coupon BEFORE any stock mutation so invalid coupons never touch stock.
                var couponResult = await _pricingService
                    .ValidateAndApplyCouponAsync(request.CouponCode, subtotal, cancellationToken);

                if (!couponResult.IsValid)
                    return Result<OrderDto>.Falid(null, couponResult.Error ?? "Invalid coupon.");

                var pricing = _pricingService.ComputeTotals(subtotal, couponResult.DiscountAmount);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Concurrency-safe stock decrement: the UPDATE only deducts when enough
                    // stock is still available and returns the number of affected rows.
                    for (var i = 0; i < orderItems.Count; i++)
                    {
                        var orderItem = orderItems[i];
                        var product = lineContexts[i].Product;
                        var variant = lineContexts[i].Variant;

                        if (variant != null)
                        {
                            var previous = variant.StockQuantity;

                            if (previous < orderItem.Quantity)
                            {
                                await _unitOfWork.RollbackAsync(cancellationToken);
                                return Result<OrderDto>.Falid(null, $"Insufficient stock for {product.NameEN}.");
                            }

                            var affected = await _unitOfWork.DecrementVariantStockAtomicallyAsync(
                                variant.Id, orderItem.Quantity, cancellationToken);

                            if (affected != 1)
                            {
                                await _unitOfWork.RollbackAsync(cancellationToken);
                                return Result<OrderDto>.Falid(null, $"Insufficient stock for {product.NameEN}.");
                            }

                            variant.StockQuantity = previous - orderItem.Quantity;
                            await _lowStockAlerter.EvaluateStockAsync(product, previous, variant.StockQuantity, cancellationToken);
                        }
                        else
                        {
                            var previous = product.StockQuantity;

                            if (previous < orderItem.Quantity)
                            {
                                await _unitOfWork.RollbackAsync(cancellationToken);
                                return Result<OrderDto>.Falid(null, $"Insufficient stock for {product.NameEN}.");
                            }

                            var affected = await _unitOfWork.DecrementStockAtomicallyAsync(
                                product.Id, orderItem.Quantity, cancellationToken);

                            if (affected != 1)
                            {
                                await _unitOfWork.RollbackAsync(cancellationToken);
                                return Result<OrderDto>.Falid(null, $"Insufficient stock for {product.NameEN}.");
                            }

                            product.StockQuantity = previous - orderItem.Quantity;
                            await _lowStockAlerter.EvaluateAsync(product, previous, cancellationToken);
                        }
                    }

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
                        StockDeducted = true,
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

                    // Increment coupon usage count so the limit is enforced
                    if (couponResult.Coupon != null)
                    {
                        couponResult.Coupon.UsedCount++;
                        _unitOfWork.Repository<Coupon>().Update(couponResult.Coupon);
                    }

                    var payment = new Payment
                    {
                        OrderId = order.Id,
                        Amount = pricing.Total,
                        Method = request.PaymentMethod.ToString(),
                        Status = PaymentStatus.Pending,
                        GatewayResponse = "Payment pending — collect on delivery."
                    };
                    _unitOfWork.Repository<Payment>().Create(payment);

                    // Clear cart
                    foreach (var cartItem in cart.Items.ToList())
                    {
                        _unitOfWork.Repository<CartItem>().Delete(cartItem);
                    }

                    var result = await _unitOfWork.CompleteAsync(cancellationToken);

                    if (result <= 0)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                        return Result<OrderDto>.Falid(null, "Failed to place order.");
                    }

                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result<OrderDto>.Success(new OrderDto
                    {
                        Id = order.Id,
                        OrderNumber = order.OrderNumber,
                        Subtotal = order.Subtotal,
                        DiscountAmount = order.DiscountAmount,
                        Tax = order.Tax,
                        ShippingCost = order.ShippingCost,
                        Total = order.Total,
                        Status = order.Status.ToString(),
                        ShippingAddress = order.ShippingAddress,
                        Items = orderItems.Select(oi => new OrderItemDto
                        {
                            ProductId = oi.ProductId,
                            VariantId = oi.VariantId,
                            VariantLabel = oi.VariantLabel,
                            ProductName = oi.ProductName,
                            ProductImageUrl = oi.ProductImageUrl,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            TotalPrice = oi.TotalPrice
                        }).ToList(),
                        CreatedAt = order.CreatedAt
                    }, "Order placed successfully.");
                }
                catch
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    throw;
                }
            }

            private string GenerateOrderNumber()
            {
                return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            }
        }
    }
}