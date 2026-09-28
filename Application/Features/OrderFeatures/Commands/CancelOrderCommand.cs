using Domain.Entities.OrderEntities;
using Domain.Entities.NotificationEntities;
using Application.Services;
using Infrastructure.Services.Realtime;

namespace Application.Features.OrderFeatures.Commands
{
    public class CancelOrderCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string OrderId { get; set; } = string.Empty;

        public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;
            private readonly IRealtimeNotifier _notifier;
            private readonly IStockRestorer _stockRestorer;

            public CancelOrderCommandHandler(
                IUnitOfWork unitOfWork,
                ICurrentUserService currentUserService,
                IRealtimeNotifier notifier,
                IStockRestorer stockRestorer)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
                _notifier = notifier;
                _stockRestorer = stockRestorer;
            }

            public async Task<Result<string>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                var order = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.Id == request.OrderId && o.UserId == _currentUserService.UserId)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order == null)
                    return Result<string>.Falid(null, "Order not found.");

                if (order.Status != OrderStatus.Pending)
                    return Result<string>.Falid(null, "Only pending orders can be cancelled.");

                await _stockRestorer.RestoreAsync(order, cancellationToken);

                order.Status = OrderStatus.Cancelled;
                order.LastModifiedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Order>().Update(order);

                var notification = new Notification
                {
                    UserId = order.UserId,
                    Title = $"Order {order.OrderNumber} has been cancelled",
                    Message = $"Your order {order.OrderNumber} has been cancelled.",
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
                    return Result<string>.Success(null, "Order cancelled successfully.");
                }

                return Result<string>.Falid(null, "Failed to cancel order.");
            }
        }
    }
}
