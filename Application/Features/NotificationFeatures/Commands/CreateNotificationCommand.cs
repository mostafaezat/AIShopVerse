using Domain.Entities.NotificationEntities;

namespace Application.Features.NotificationFeatures.Commands
{
    public class CreateNotificationCommand : IRequest<Result<string>>
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public NotificationType Type { get; set; } = NotificationType.Info;
        public string? OrderId { get; set; }

        public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public CreateNotificationCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(request.UserId))
                    return Result<string>.Falid(null, "User is required.");

                var notification = new Notification
                {
                    UserId = request.UserId,
                    Title = request.Title,
                    Message = request.Message,
                    Type = request.Type,
                    OrderId = request.OrderId,
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<Notification>().Create(notification);
                await _unitOfWork.CompleteAsync(cancellationToken);

                return Result<string>.Success(notification.Id);
            }
        }
    }

    public class MarkNotificationReadCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NotificationId { get; set; } = string.Empty;

        public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public MarkNotificationReadCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<string>> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                var notification = await _unitOfWork.Repository<Notification>()
                    .FindByCondition(n => n.Id == request.NotificationId && n.UserId == _currentUserService.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (notification == null)
                    return Result<string>.Falid(null, "Notification not found.");

                notification.IsRead = true;
                _unitOfWork.Repository<Notification>().Update(notification);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, "Notification marked as read.")
                    : Result<string>.Falid(null, "Failed to update notification.");
            }
        }
    }

    public class MarkAllNotificationsReadCommand : IRequest<Result<string>>
    {
        public class MarkAllNotificationsReadCommandHandler : IRequestHandler<MarkAllNotificationsReadCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public MarkAllNotificationsReadCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<string>> Handle(MarkAllNotificationsReadCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                var unread = await _unitOfWork.Repository<Notification>()
                    .FindByCondition(n => n.UserId == _currentUserService.UserId && !n.IsRead)
                    .ToListAsync(cancellationToken);

                foreach (var notification in unread)
                {
                    notification.IsRead = true;
                    _unitOfWork.Repository<Notification>().Update(notification);
                }

                await _unitOfWork.CompleteAsync(cancellationToken);
                return Result<string>.Success(null, "All notifications marked as read.");
            }
        }
    }
}
