using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.PaymentGateway
{
    /// <summary>
    /// Thin loop that periodically asks <see cref="IExpiredOrderProcessor"/> to expire abandoned
    /// Pending card orders. The business logic lives in Application (testable); this host wrapper
    /// only manages the interval. Idempotent + restart-safe: each pass targets only Pending orders
    /// older than the cutoff, so both server apps can safely register it against a shared database.
    /// </summary>
    public class ExpiredPendingOrdersHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<PaymentOptions> _options;
        private readonly ILogger<ExpiredPendingOrdersHostedService> _logger;

        public ExpiredPendingOrdersHostedService(
            IServiceScopeFactory scopeFactory,
            IOptions<PaymentOptions> options,
            ILogger<ExpiredPendingOrdersHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IExpiredOrderProcessor>();
                    var processed = await processor.ProcessAsync(stoppingToken);
                    if (processed > 0)
                        _logger.LogInformation("Expired {Count} abandoned pending order(s).", processed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Pending-order expiry pass failed.");
                }

                var interval = TimeSpan.FromMinutes(Math.Max(1, _options.Value.OrderExpiryIntervalMinutes));
                await Task.Delay(interval, stoppingToken);
            }
        }
    }
}