using Application.Features.OrderFeatures.Queries;
using Application.Features.InventoryFeatures;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Domain.Entities.PromotionEntities;
using Domain.Entities.NotificationEntities;
using Infrastructure.Services.PaymentGateway;
using Infrastructure.Services.Realtime;

namespace Application.Services
{
    public interface IPaymentCompletionService
    {
        Task<Result<OrderDto>> CompleteAsync(string orderId, string paymentIntentId, CancellationToken cancellationToken);
    }

    public class PaymentCompletionService : IPaymentCompletionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripePaymentService _stripePayment;
        private readonly IRealtimeNotifier _notifier;
        private readonly ILowStockAlerter _lowStockAlerter;

        public PaymentCompletionService(
            IUnitOfWork unitOfWork,
            IStripePaymentService stripePayment,
            IRealtimeNotifier notifier,
            ILowStockAlerter lowStockAlerter)
        {
            _unitOfWork = unitOfWork;
            _stripePayment = stripePayment;
            _notifier = notifier;
            _lowStockAlerter = lowStockAlerter;
        }

        public async Task<Result<OrderDto>> CompleteAsync(string orderId, string paymentIntentId, CancellationToken cancellationToken)
        {
            var order = await _unitOfWork.Repository<Order>()
                .FindByCondition(o => o.Id == orderId)
                .Include(o => o.Items)
                .FirstOrDefaultAsync(cancellationToken);

            if (order == null)
                return Result<OrderDto>.Falid(null, "Order not found.");

            if (order.Status == OrderStatus.Paid)
                return Result<OrderDto>.Success(ToDto(order), "Order already completed.");

            if (order.Status != OrderStatus.Pending)
                return Result<OrderDto>.Falid(null, "Order is not in a payable state.");

            var payment = await _unitOfWork.Repository<Payment>()
                .FindByCondition(p => p.OrderId == order.Id && p.TransactionId == paymentIntentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (payment == null)
                return Result<OrderDto>.Falid(null, "Payment record not found for this order.");

            if (_stripePayment.IsEnabled)
            {
                var succeeded = await _stripePayment.IsPaymentSucceededAsync(paymentIntentId, cancellationToken);
                if (!succeeded)
                {
                    payment.Status = PaymentStatus.Failed;
                    order.Status = OrderStatus.Cancelled;
                    order.LastModifiedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<Notification>().Create(new Notification
                    {
                        UserId = order.UserId,
                        Title = $"Payment failed for order {order.OrderNumber}",
                        Message = "Your payment could not be processed and the order was cancelled.",
                        Type = NotificationType.OrderStatus,
                        OrderId = order.Id,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _unitOfWork.CompleteAsync(cancellationToken);

                    await _notifier.NotifyUserAsync(
                        RealtimeEventKind.OrderStatus,
                        order.UserId,
                        new { orderId = order.Id, status = order.Status.ToString() },
                        cancellationToken);

                    return Result<OrderDto>.Falid(null, "Payment was not successful.");
                }
            }

            order.Status = OrderStatus.Paid;
            order.LastModifiedAt = DateTime.UtcNow;
            order.StockDeducted = true;

            payment.Status = PaymentStatus.Completed;
            payment.PaidAt = DateTime.UtcNow;
            payment.GatewayResponse = _stripePayment.IsEnabled
                ? $"Stripe payment succeeded (intent {paymentIntentId})."
                : "Mock card payment completed at checkout.";

            await DecrementStockAsync(order, cancellationToken);
            await ClearCartAsync(order.UserId, cancellationToken);
            await IncrementCouponUsageAsync(order.CouponCode, cancellationToken);

            var result = await _unitOfWork.CompleteAsync(cancellationToken);
            if (result <= 0)
                return Result<OrderDto>.Falid(null, "Failed to complete payment.");

            await _notifier.NotifyAdminsAsync(
                RealtimeEventKind.NewOrder,
                new { orderId = order.Id, orderNumber = order.OrderNumber, status = order.Status.ToString(), total = order.Total },
                cancellationToken);

            return Result<OrderDto>.Success(ToDto(order), "Payment completed successfully.");
        }

        private async Task DecrementStockAsync(Order order, CancellationToken cancellationToken)
        {
            var productRepo = _unitOfWork.Repository<Product>();
            var variantRepo = _unitOfWork.Repository<ProductVariant>();
            foreach (var item in order.Items)
            {
                if (!string.IsNullOrEmpty(item.VariantId))
                {
                    var variant = await variantRepo.FindByCondition(v => v.Id == item.VariantId)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (variant != null)
                    {
                        variant.StockQuantity = Math.Max(0, variant.StockQuantity - item.Quantity);
                        variantRepo.Update(variant);
                    }
                }
                else
                {
                    var product = await productRepo.FindByCondition(p => p.Id == item.ProductId)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (product != null)
                    {
                        var previous = product.StockQuantity;
                        product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                        product.LastModifiedAt = DateTime.UtcNow;
                        productRepo.Update(product);
                        await _lowStockAlerter.EvaluateAsync(product, previous, cancellationToken);
                    }
                }
            }
        }

        private async Task ClearCartAsync(string userId, CancellationToken cancellationToken)
        {
            var cart = await _unitOfWork.Repository<Cart>()
                .FindByCondition(c => c.UserId == userId)
                .Include(c => c.Items)
                .FirstOrDefaultAsync(cancellationToken);

            if (cart != null)
            {
                var cartItemRepo = _unitOfWork.Repository<CartItem>();
                foreach (var cartItem in cart.Items.ToList())
                    cartItemRepo.Delete(cartItem);
            }
        }

        private async Task IncrementCouponUsageAsync(string? couponCode, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(couponCode)) return;

            var coupon = await _unitOfWork.Repository<Coupon>()
                .FindByCondition(c => c.Code.ToLower() == couponCode.Trim().ToLower())
                .FirstOrDefaultAsync(cancellationToken);

            if (coupon != null)
            {
                coupon.UsedCount++;
                _unitOfWork.Repository<Coupon>().Update(coupon);
            }
        }

        private static OrderDto ToDto(Order order)
        {
            return new OrderDto
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
                CouponCode = order.CouponCode,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    VariantId = i.VariantId,
                    VariantLabel = i.VariantLabel,
                    ProductName = i.ProductName,
                    ProductImageUrl = i.ProductImageUrl,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };
        }
    }
}
