using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Domain.Entities.NotificationEntities;
using Application.Features.OrderFeatures;
using Application.Services;
using Infrastructure.Services.PaymentGateway;
using Infrastructure.Services.Realtime;

using Microsoft.EntityFrameworkCore;

namespace Application.Features.PaymentFeatures.Commands
{
    public class RefundOrderCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string OrderId { get; set; } = string.Empty;

        public class RefundOrderCommandHandler : IRequestHandler<RefundOrderCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IStripePaymentService _stripePayment;
            private readonly IRealtimeNotifier _notifier;
            private readonly IStockRestorer _stockRestorer;

            public RefundOrderCommandHandler(IUnitOfWork unitOfWork, IStripePaymentService stripePayment, IRealtimeNotifier notifier, IStockRestorer stockRestorer)
            {
                _unitOfWork = unitOfWork;
                _stripePayment = stripePayment;
                _notifier = notifier;
                _stockRestorer = stockRestorer;
            }

            public async Task<Result<string>> Handle(RefundOrderCommand request, CancellationToken cancellationToken)
            {
                var order = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.Id == request.OrderId)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order == null)
                    return Result<string>.Falid(null, "Order not found.");

                if (order.Status == OrderStatus.Refunded)
                    return Result<string>.Falid(null, "Order is already refunded.");

                if (!OrderStatusTransitions.Can(order.Status, OrderStatus.Refunded))
                    return Result<string>.Falid(null, $"Only paid orders can be refunded (current status: {order.Status}).");

                var payment = await _unitOfWork.Repository<Payment>()
                    .FindByCondition(p => p.OrderId == order.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (payment == null)
                    return Result<string>.Falid(null, "No payment record for this order.");

                if (payment.Status == PaymentStatus.Refunded)
                    return Result<string>.Falid(null, "Payment is already refunded.");

                string? refundId = null;
                if (_stripePayment.IsEnabled && !string.IsNullOrEmpty(payment.TransactionId) &&
                    !payment.TransactionId.StartsWith("MOCK-", StringComparison.OrdinalIgnoreCase))
                {
                    refundId = await _stripePayment.RefundAsync(payment.TransactionId, null, cancellationToken);
                    if (refundId == null)
                        return Result<string>.Falid(null, "Stripe refund failed.");
                }

                payment.Status = PaymentStatus.Refunded;
                payment.GatewayResponse = refundId != null
                    ? $"Refunded via Stripe (refund {refundId})."
                    : "Refunded (record-only; no Stripe keys configured).";

                order.Status = OrderStatus.Refunded;
                order.LastModifiedAt = DateTime.UtcNow;

                await _stockRestorer.RestoreAsync(order, cancellationToken);

                _unitOfWork.Repository<Payment>().Update(payment);
                _unitOfWork.Repository<Order>().Update(order);

                _unitOfWork.Repository<Notification>().Create(new Notification
                {
                    UserId = order.UserId,
                    Title = $"Order {order.OrderNumber} refunded",
                    Message = "Your order has been refunded.",
                    Type = NotificationType.OrderStatus,
                    OrderId = order.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                var result = await _unitOfWork.CompleteAsync(cancellationToken);
                if (result <= 0)
                    return Result<string>.Falid(null, "Failed to record refund.");

                await _notifier.NotifyUserAsync(
                    RealtimeEventKind.OrderStatus,
                    order.UserId,
                    new { orderId = order.Id, status = order.Status.ToString() },
                    cancellationToken);

                return Result<string>.Success("Order refunded successfully.", "Order refunded successfully.");
            }
        }
    }
}
