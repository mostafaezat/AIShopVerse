using Domain.Entities.NotificationEntities;

namespace Infrastructure.Services.Realtime
{
    public interface IRealtimeNotifier
    {
        /// <summary>Push a realtime event to a specific user's clients (available on all hosts via relay).</summary>
        Task NotifyUserAsync(RealtimeEventKind kind, string userId, object? payload, CancellationToken cancellationToken = default);

        /// <summary>Push a realtime event to the 'admins' group (available on all hosts via relay).</summary>
        Task NotifyAdminsAsync(RealtimeEventKind kind, object? payload, CancellationToken cancellationToken = default);
    }
}
