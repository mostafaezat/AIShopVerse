using Domain.Entities.NotificationEntities;
using Domain.Entities.Identity;

namespace Application.Features.NotificationFeatures.Queries
{
    public class GetMyNotificationsQuery : IRequest<Result<List<NotificationDto>>>
    {
        public bool? OnlyUnread { get; set; }
        public int? Take { get; set; }

        public class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, Result<List<NotificationDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public GetMyNotificationsQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<List<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<List<NotificationDto>>.Falid(null, "User not authenticated.");

                var query = _unitOfWork.Repository<Notification>()
                    .FindByCondition(n => n.UserId == _currentUserService.UserId);

                if (request.OnlyUnread == true)
                    query = query.Where(n => !n.IsRead);

                return Result<List<NotificationDto>>.Success(await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationDto
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        OrderId = n.OrderId,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    })
                    .Take(request.Take ?? 50)
                    .ToListAsync(cancellationToken));
            }
        }
    }

    public class GetUnreadNotificationsCountQuery : IRequest<Result<int>>
    {
        public class GetUnreadNotificationsCountQueryHandler : IRequestHandler<GetUnreadNotificationsCountQuery, Result<int>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public GetUnreadNotificationsCountQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<int>> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<int>.Falid(0, "User not authenticated.");

                var count = await _unitOfWork.Repository<Notification>()
                    .FindByCondition(n => n.UserId == _currentUserService.UserId && !n.IsRead)
                    .CountAsync(cancellationToken);

                return Result<int>.Success(count);
            }
        }
    }

    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Message { get; set; }
        public NotificationType Type { get; set; }
        public string? OrderId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
