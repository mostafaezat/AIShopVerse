using Domain.Entities.NotificationEntities;
using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services.Realtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.PaymentGateway
{
    /// <summary>
    /// Cancels abandoned card (Stripe) checkouts whose order has stayed Pending with an unpaid
    /// CreditCard payment past the configurable timeout. COD orders stay Pending until delivery and
    /// are deliberately NOT expired. Card checkouts created via CreateCheckoutCommand never deduct
    /// stock at the Pending stage (deduction happens only when payment succeeds), so no stock restore
    /// is needed here. Idempotent: only Pending orders are targeted, so concurrent/duplicate passes
    /// (e.g. both server apps registering the worker against a shared database) are safe. Restart-safe:
    /// each pass starts from the persisted CreatedAt cutoff.
    /// </summary>
    public interface IExpiredOrderProcessor
    {
        Task<int> ProcessAsync(CancellationToken cancellationToken = default);
    }

    public class ExpiredOrderProcessor : IExpiredOrderProcessor
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotifier _notifier;
        private readonly IOptions<PaymentOptions> _options;
        private readonly ILogger<ExpiredOrderProcessor> _logger;

        public ExpiredOrderProcessor(
            IUnitOfWork unitOfWork,
            IRealtimeNotifier notifier,
            IOptions<PaymentOptions> options,
            ILogger<ExpiredOrderProcessor> logger)
        {
            _unitOfWork = unitOfWork;
            _notifier = notifier;
            _options = options;
            _logger = logger;
        }

        public async Task<int> ProcessAsync(CancellationToken cancellationToken = default)
        {
            var timeout = TimeSpan.FromMinutes(_options.Value.PendingOrderTimeoutMinutes);
            var cutoff = DateTime.UtcNow - timeout;

            var orderRepo = _unitOfWork.Repository<Order>();
            var paymentRepo = _unitOfWork.Repository<Payment>();
            var card = PaymentMethod.CreditCard.ToString();

            var expiredOrderIds = await paymentRepo
                .FindByCondition(p => p.Status == PaymentStatus.Pending && p.Method == card)
                .Select(p => p.OrderId)
                .ToListAsync(cancellationToken);

            var expired = await orderRepo
                .FindByCondition(o => o.Status == OrderStatus.Pending
                    && o.CreatedAt < cutoff
                    && expiredOrderIds.Contains(o.Id))
                .Include(o => o.Items)
                .ToListAsync(cancellationToken);

            foreach (var order in expired)
            {
                order.Status = OrderStatus.Cancelled;
                order.LastModifiedAt = DateTime.UtcNow;
                orderRepo.Update(order);

                var pendingPayments = await paymentRepo
                    .FindByCondition(p => p.OrderId == order.Id && p.Status == PaymentStatus.Pending)
                    .ToListAsync(cancellationToken);
                foreach (var p in pendingPayments)
                {
                    p.Status = PaymentStatus.Failed;
                    p.GatewayResponse = "Order expired before payment was completed.";
                    paymentRepo.Update(p);
                }

                _unitOfWork.Repository<Notification>().Create(new Notification
                {
                    UserId = order.UserId,
                    Title = $"Order {order.OrderNumber} expired",
                    Message = $"Your order {order.OrderNumber} was not completed in time and has been cancelled.",
                    Type = NotificationType.OrderStatus,
                    OrderId = order.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _notifier.NotifyUserAsync(
                    RealtimeEventKind.OrderStatus,
                    order.UserId,
                    new { orderId = order.Id, status = order.Status.ToString() },
                    cancellationToken);
            }

            if (expired.Count > 0)
                await _unitOfWork.CompleteAsync(cancellationToken);

            if (expired.Count > 0)
                _logger.LogInformation("Expired {Count} abandoned pending order(s).", expired.Count);

            return expired.Count;
        }
    }
}