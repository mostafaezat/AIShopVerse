using Domain.Entities.OrderEntities;
using Domain.Entities.NotificationEntities;
using Domain.Entities.PaymentEntities;
using Application.Services;
using Infrastructure.Services.Realtime;

namespace Application.Features.OrderFeatures.Commands
{
    public class UpdateOrderStatusCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string OrderId { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public OrderStatus NewStatus { get; set; }

        public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IRealtimeNotifier _notifier;
            private readonly IStockRestorer _stockRestorer;

            public UpdateOrderStatusCommandHandler(IUnitOfWork unitOfWork, IRealtimeNotifier notifier, IStockRestorer stockRestorer)
            {
                _unitOfWork = unitOfWork;
                _notifier = notifier;
                _stockRestorer = stockRestorer;
            }

            public async Task<Result<string>> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
            {
                var order = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.Id == request.OrderId)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order == null)
                    return Result<string>.Falid(null, "Order not found.");

                if (order.Status == request.NewStatus)
                    return Result<string>.Falid(null, $"Order is already {order.Status}.");

                if (!OrderStatusTransitions.Can(order.Status, request.NewStatus))
                    return Result<string>.Falid(null, $"Invalid status transition from {order.Status} to {request.NewStatus}.");

                order.Status = request.NewStatus;
                order.LastModifiedAt = DateTime.UtcNow;

                if (request.NewStatus == OrderStatus.Cancelled)
                    await _stockRestorer.RestoreAsync(order, cancellationToken);

                _unitOfWork.Repository<Order>().Update(order);

                if (request.NewStatus == OrderStatus.Delivered)
                {
                    var payment = await _unitOfWork.Repository<Payment>()
                        .FindByCondition(p => p.OrderId == order.Id)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (payment != null
                        && string.Equals(payment.Method, nameof(PaymentMethod.CashOnDelivery), StringComparison.OrdinalIgnoreCase)
                        && payment.Status == PaymentStatus.Pending)
                    {
                        payment.Status = PaymentStatus.Completed;
                        payment.PaidAt = DateTime.UtcNow;
                        payment.GatewayResponse = "Collected on delivery.";
                        _unitOfWork.Repository<Payment>().Update(payment);
                    }
                }

                var notification = new Notification
                {
                    UserId = order.UserId,
                    Title = $"Order {order.OrderNumber} is now {order.Status}",
                    Message = $"Your order {order.OrderNumber} status has been updated to {order.Status}.",
                    Type = NotificationType.OrderStatus,
                    OrderId = order.Id,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _unitOfWork.Repository<Notification>().Create(notification);

                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                if (result > 0)
                {
                    await _notifier.NotifyUserAsync(
                        RealtimeEventKind.OrderStatus,
                        order.UserId,
                        new { orderId = order.Id, status = order.Status.ToString() },
                        cancellationToken);
                    return Result<string>.Success(null, "Order status updated.");
                }

                return Result<string>.Falid(null, "Failed to update order status.");
            }
        }
    }
}
