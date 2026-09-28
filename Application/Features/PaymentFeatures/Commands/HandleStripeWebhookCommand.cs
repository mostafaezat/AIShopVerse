using Application.Services;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Domain.Entities.NotificationEntities;
using Infrastructure.Services.PaymentGateway;
using Infrastructure.Services.Realtime;

namespace Application.Features.PaymentFeatures.Commands
{
    public class HandleStripeWebhookCommand : IRequest<Result<string>>
    {
        public string Json { get; set; } = string.Empty;
        public string Signature { get; set; } = string.Empty;

        public class HandleStripeWebhookCommandHandler : IRequestHandler<HandleStripeWebhookCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IStripePaymentService _stripePayment;
            private readonly IPaymentCompletionService _completionService;
            private readonly IRealtimeNotifier _notifier;

            public HandleStripeWebhookCommandHandler(
                IUnitOfWork unitOfWork,
                IStripePaymentService stripePayment,
                IPaymentCompletionService completionService,
                IRealtimeNotifier notifier)
            {
                _unitOfWork = unitOfWork;
                _stripePayment = stripePayment;
                _completionService = completionService;
                _notifier = notifier;
            }

            public async Task<Result<string>> Handle(HandleStripeWebhookCommand request, CancellationToken cancellationToken)
            {
                var webhook = _stripePayment.ReadWebhook(request.Json, request.Signature);
                if (webhook == null || string.IsNullOrEmpty(webhook.PaymentIntentId))
                    return Result<string>.Falid(null, "Invalid webhook signature.");

                var payment = await _unitOfWork.Repository<Payment>()
                    .FindByCondition(p => p.TransactionId == webhook.PaymentIntentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (payment == null)
                    return Result<string>.Success($"No local payment for intent {webhook.PaymentIntentId}; ignored.");

                var order = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.Id == payment.OrderId)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order == null)
                    return Result<string>.Success("Order not found; ignored.");

                if (webhook.Type == "payment_intent.succeeded")
                {
                    if (order.Status != OrderStatus.Paid)
                    {
                        var completion = await _completionService.CompleteAsync(order.Id, webhook.PaymentIntentId, cancellationToken);
                        if (!completion.IsSuccess)
                            return Result<string>.Falid(null, completion.Message ?? "Failed to complete order.");
                    }
                    return Result<string>.Success("Payment succeeded and order completed.");
                }

                if (webhook.Type == "payment_intent.payment_failed")
                {
                    payment.Status = PaymentStatus.Failed;
                    payment.GatewayResponse = "Stripe reported payment failure via webhook.";
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

                    return Result<string>.Success("Payment failure recorded.");
                }

                if (webhook.Type == "charge.refunded")
                {
                    payment.Status = PaymentStatus.Refunded;
                    payment.GatewayResponse = "Stripe reported refund via webhook.";
                    order.Status = OrderStatus.Refunded;
                    order.LastModifiedAt = DateTime.UtcNow;

                    _unitOfWork.Repository<Notification>().Create(new Notification
                    {
                        UserId = order.UserId,
                        Title = $"Refund for order {order.OrderNumber}",
                        Message = "Your order has been refunded.",
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

                    return Result<string>.Success("Refund recorded.");
                }

                return Result<string>.Success($"Unhandled webhook type: {webhook.Type}");
            }
        }
    }
}
