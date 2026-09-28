using System.Text.Json;
using Domain.Entities.NotificationEntities;
using Infrastructure.Persistence;
using Infrastructure.Signalr;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Realtime
{
    public class RealtimeRelayHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RedisRelayOptions _relayOptions;
        private readonly ILogger<RealtimeRelayHostedService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(2);

        public RealtimeRelayHostedService(
            IServiceScopeFactory scopeFactory,
            RedisRelayOptions relayOptions,
            ILogger<RealtimeRelayHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _relayOptions = relayOptions;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // No outbox needed when a Redis backplane relays events across hosts.
            if (_relayOptions.Enabled)
                return;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RelayPendingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Realtime relay pass failed.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task RelayPendingAsync(CancellationToken cancellationToken)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var hub = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationHub>>();

            var pending = await db.RealtimeEvents
                .Where(e => e.ProcessedAt == null && e.Source != _relayOptions.HostName)
                .OrderBy(e => e.CreatedAt)
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var evt in pending)
            {
                object? payload = null;
                if (!string.IsNullOrEmpty(evt.PayloadJson))
                {
                    try { payload = JsonSerializer.Deserialize<object>(evt.PayloadJson); }
                    catch { payload = null; }
                }

                var eventName = SignalRRealtimeNotifier.EventName(evt.Kind);

                if (!string.IsNullOrEmpty(evt.TargetUserId))
                {
                    await hub.Clients.User(evt.TargetUserId).SendAsync(eventName, payload ?? new { }, cancellationToken);
                }
                else
                {
                    await hub.Clients.Group("admins").SendAsync(eventName, payload ?? new { }, cancellationToken);
                }

                evt.ProcessedAt = DateTime.UtcNow;
                db.RealtimeEvents.Update(evt);
            }

            if (pending.Any())
                await db.SaveChangesAsync(cancellationToken);
        }
    }
}
