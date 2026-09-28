using Domain.Entities.NotificationEntities;
using Infrastructure.Services.Realtime;

namespace Application.Features.NotificationFeatures.Commands
{
    public class BroadcastNotificationCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string Title { get; set; } = string.Empty;

        public string? Message { get; set; }

        public NotificationType Type { get; set; } = NotificationType.Info;

        public class BroadcastNotificationCommandHandler : IRequestHandler<BroadcastNotificationCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly IRealtimeNotifier _notifier;

            public BroadcastNotificationCommandHandler(IUnitOfWork unitOfWork, IRealtimeNotifier notifier)
            {
                _unitOfWork = unitOfWork;
                _notifier = notifier;
            }

            public async Task<Result<string>> Handle(BroadcastNotificationCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrWhiteSpace(request.Title))
                    return Result<string>.Falid(null, "Title is required.");

                var userIds = await _unitOfWork.Repository<ApplicationUser>()
                    .FindAll()
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

                foreach (var userId in userIds)
                {
                    _unitOfWork.Repository<Notification>().Create(new Notification
                    {
                        UserId = userId,
                        Title = request.Title,
                        Message = request.Message,
                        Type = request.Type,
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _unitOfWork.CompleteAsync(cancellationToken);

                foreach (var userId in userIds)
                {
                    await _notifier.NotifyUserAsync(
                        RealtimeEventKind.Info,
                        userId,
                        new { title = request.Title, message = request.Message, type = (int)request.Type },
                        cancellationToken);
                }

                return Result<string>.Success(null, "Notification broadcast.");
            }
        }
    }
}
