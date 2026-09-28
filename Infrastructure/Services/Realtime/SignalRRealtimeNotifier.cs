using System.Text.Json;
using Domain.Entities.NotificationEntities;
using Infrastructure.Persistence;
using Infrastructure.Signalr;
using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Services.Realtime
{
    public class SignalRRealtimeNotifier : IRealtimeNotifier
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ApplicationDbContext _dbContext;
        private readonly RedisRelayOptions _relayOptions;

        public SignalRRealtimeNotifier(
            IHubContext<NotificationHub> hubContext,
            ApplicationDbContext dbContext,
            RedisRelayOptions relayOptions)
        {
            _hubContext = hubContext;
            _dbContext = dbContext;
            _relayOptions = relayOptions;
        }

        public async Task NotifyUserAsync(RealtimeEventKind kind, string userId, object? payload, CancellationToken cancellationToken = default)
        {
            if (!_relayOptions.Enabled)
            {
                _dbContext.RealtimeEvents.Add(new RealtimeEvent
                {
                    Kind = kind,
                    TargetUserId = userId,
                    PayloadJson = payload == null ? null : JsonSerializer.Serialize(payload),
                    Source = _relayOptions.HostName,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var name = EventName(kind);
            await _hubContext.Clients.User(userId)
                .SendAsync(name, payload ?? new { }, cancellationToken);

            if (!_relayOptions.Enabled)
                await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task NotifyAdminsAsync(RealtimeEventKind kind, object? payload, CancellationToken cancellationToken = default)
        {
            if (!_relayOptions.Enabled)
            {
                _dbContext.RealtimeEvents.Add(new RealtimeEvent
                {
                    Kind = kind,
                    PayloadJson = payload == null ? null : JsonSerializer.Serialize(payload),
                    Source = _relayOptions.HostName,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var name = EventName(kind);
            await _hubContext.Clients.Group("admins")
                .SendAsync(name, payload ?? new { }, cancellationToken);

            if (!_relayOptions.Enabled)
                await _dbContext.SaveChangesAsync(cancellationToken);
        }

        internal static string EventName(RealtimeEventKind kind) => kind switch
        {
            RealtimeEventKind.OrderStatus => "OrderStatusUpdate",
            RealtimeEventKind.NewOrder => "NewOrder",
            RealtimeEventKind.LowStock => "LowStockAlert",
            RealtimeEventKind.BackInStock => "BackInStock",
            _ => "Info"
        };
    }
}
