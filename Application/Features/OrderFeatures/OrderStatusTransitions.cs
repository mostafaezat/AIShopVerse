using Domain.Entities.OrderEntities;

namespace Application.Features.OrderFeatures
{
    /// <summary>
    /// Single source of truth for the order status transition matrix (P1-2).
    /// Same-status transitions are never allowed. Terminal states: Delivered,
    /// Cancelled, Refunded.
    /// </summary>
    public static class OrderStatusTransitions
    {
        private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Allowed = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Pending] = new[] { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Cancelled },
            [OrderStatus.Paid] = new[] { OrderStatus.Refunded },
            [OrderStatus.Processing] = new[] { OrderStatus.Shipped, OrderStatus.Cancelled },
            [OrderStatus.Shipped] = new[] { OrderStatus.Delivered, OrderStatus.Cancelled },
            [OrderStatus.Delivered] = Array.Empty<OrderStatus>(),
            [OrderStatus.Cancelled] = Array.Empty<OrderStatus>(),
            [OrderStatus.Refunded] = Array.Empty<OrderStatus>()
        };

        public static bool Can(OrderStatus from, OrderStatus to)
            => from != to
               && Allowed.TryGetValue(from, out var targets)
               && Array.IndexOf(targets, to) >= 0;

        public static IReadOnlyCollection<OrderStatus> Targets(OrderStatus from)
            => Allowed.TryGetValue(from, out var targets) ? targets : Array.Empty<OrderStatus>();
    }
}