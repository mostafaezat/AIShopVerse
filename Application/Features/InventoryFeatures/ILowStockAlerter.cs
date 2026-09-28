using Domain.Entities.CatalogEntities;
using Domain.Entities.NotificationEntities;
using Infrastructure.Services;
using Infrastructure.Services.Realtime;
using Microsoft.Extensions.Options;

namespace Application.Features.InventoryFeatures
{
    public class LowStockPayload
    {
        public string ProductId { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public int Threshold { get; set; }
    }

    public interface ILowStockAlerter
    {
        Task EvaluateAsync(Product product, int previousQuantity, CancellationToken cancellationToken = default);

        Task EvaluateStockAsync(Product product, int previousQuantity, int currentQuantity, CancellationToken cancellationToken = default);
    }

    public class LowStockAlerter : ILowStockAlerter
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotifier _notifier;
        private readonly InventoryOptions _options;

        public LowStockAlerter(IUnitOfWork unitOfWork, IRealtimeNotifier notifier, IOptions<InventoryOptions> options)
        {
            _unitOfWork = unitOfWork;
            _notifier = notifier;
            _options = options.Value;
        }

        public Task EvaluateAsync(Product product, int previousQuantity, CancellationToken cancellationToken = default)
        {
            return EvaluateStockAsync(product, previousQuantity, product.StockQuantity, cancellationToken);
        }

        public async Task EvaluateStockAsync(Product product, int previousQuantity, int currentQuantity, CancellationToken cancellationToken = default)
        {
            var threshold = product.LowStockThreshold ?? _options.LowStockThreshold;
            var newQuantity = currentQuantity;

            var crossedLow = previousQuantity > threshold && newQuantity <= threshold;
            var crossedBack = previousQuantity <= threshold && newQuantity > threshold;

            if (!crossedLow && !crossedBack)
                return;

            var kind = crossedLow ? RealtimeEventKind.LowStock : RealtimeEventKind.BackInStock;
            var payload = new LowStockPayload
            {
                ProductId = product.Id,
                NameEN = product.NameEN,
                SKU = product.SKU,
                StockQuantity = newQuantity,
                Threshold = threshold
            };

            var notification = new Notification
            {
                Title = crossedLow
                    ? $"Low stock: {product.NameEN}"
                    : $"Back in stock: {product.NameEN}",
                Message = crossedLow
                    ? $"{product.NameEN} ({product.SKU}) is at {newQuantity}, at or below the threshold of {threshold}."
                    : $"{product.NameEN} ({product.SKU}) is back above the threshold of {threshold} (now {newQuantity}).",
                Type = NotificationType.Sale,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.Repository<Notification>().Create(notification);
            await _notifier.NotifyAdminsAsync(kind, payload, cancellationToken);
        }
    }
}
