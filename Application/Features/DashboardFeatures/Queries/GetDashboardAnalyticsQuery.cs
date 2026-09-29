using Domain.Entities.OrderEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.ReviewEntities;

namespace Application.Features.DashboardFeatures.Queries
{
    public class GetDashboardAnalyticsQuery : IRequest<Result<DashboardAnalyticsDto>>
    {
        public int? Days { get; set; }
        public int TopProductCount { get; set; } = 5;

        public class GetDashboardAnalyticsQueryHandler : IRequestHandler<GetDashboardAnalyticsQuery, Result<DashboardAnalyticsDto>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetDashboardAnalyticsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<DashboardAnalyticsDto>> Handle(GetDashboardAnalyticsQuery request, CancellationToken cancellationToken)
            {
                var fromDate = request.Days.HasValue
                    ? DateTime.UtcNow.AddDays(-request.Days.Value)
                    : DateTime.MinValue;

                var ordersQuery = _unitOfWork.Repository<Order>().FindAll().AsNoTracking();

                if (request.Days.HasValue)
                    ordersQuery = ordersQuery.Where(o => o.CreatedAt >= fromDate);

                var orders = await ordersQuery
                    .OrderByDescending(o => o.CreatedAt)
                    .ToListAsync(cancellationToken);

                var countedStatuses = new[] { OrderStatus.Cancelled, OrderStatus.Refunded };
                var revenueOrders = orders.Where(o => !countedStatuses.Contains(o.Status));

                var totalRevenue = revenueOrders.Sum(o => o.Total);
                var totalOrders = orders.Count;

                var topProductCount = Math.Max(1, request.TopProductCount);

                var orderIdsInRange = orders.Select(o => o.Id).ToList();
                var orderItems = await _unitOfWork.Repository<OrderItem>()
                    .FindByCondition(i => orderIdsInRange.Contains(i.OrderId))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var topProducts = orderItems
                    .GroupBy(i => i.ProductName)
                    .Select(g => new TopProductDto
                    {
                        ProductName = g.Key,
                        UnitsSold = g.Sum(i => i.Quantity),
                        Revenue = g.Sum(i => i.TotalPrice)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(topProductCount)
                    .ToList();

                var lowStockProducts = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.StockQuantity <= 10)
                    .OrderBy(p => p.StockQuantity)
                    .Take(topProductCount)
                    .AsNoTracking()
                    .Select(p => new LowStockProductDto
                    {
                        ProductId = p.Id,
                        NameEN = p.NameEN,
                        SKU = p.SKU,
                        StockQuantity = p.StockQuantity
                    })
                    .ToListAsync(cancellationToken);

                var lowStockTotal = await _unitOfWork.Repository<Product>()
                    .FindAll().CountAsync(p => p.StockQuantity <= 10, cancellationToken);

                var totalUsers = await _unitOfWork.Repository<ApplicationUser>()
                    .FindAll().CountAsync(cancellationToken);

                var totalProducts = await _unitOfWork.Repository<Product>()
                    .FindAll().CountAsync(cancellationToken);

                var pendingReviews = await _unitOfWork.Repository<Review>()
                    .FindByCondition(r => !r.IsApproved).CountAsync(cancellationToken);

                var ordersByStatus = new List<OrderStatusCountDto>();
                foreach (var status in Enum.GetValues<OrderStatus>())
                {
                    ordersByStatus.Add(new OrderStatusCountDto
                    {
                        Status = status.ToString(),
                        Count = orders.Count(o => o.Status == status)
                    });
                }

                var revenueOverTime = BuildRevenueSeries(revenueOrders, request.Days);

                var recentOrders = orders
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(8)
                    .Select(o => new RecentOrderDto
                    {
                        OrderNumber = o.OrderNumber,
                        Status = o.Status.ToString(),
                        Total = o.Total,
                        CreatedAt = o.CreatedAt
                    })
                    .ToList();

                var result = new DashboardAnalyticsDto
                {
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0,
                    TotalUsers = totalUsers,
                    TotalProducts = totalProducts,
                    LowStockCount = lowStockTotal,
                    PendingReviewsCount = pendingReviews,
                    RevenueOverTime = revenueOverTime,
                    OrdersByStatus = ordersByStatus,
                    TopProducts = topProducts,
                    LowStockProducts = lowStockProducts,
                    RecentOrders = recentOrders
                };

                return Result<DashboardAnalyticsDto>.Success(result);
            }

            private List<RevenuePointDto> BuildRevenueSeries(IEnumerable<Order> revenueOrders, int? days)
            {
                var series = revenueOrders
                    .GroupBy(o => o.CreatedAt.Date)
                    .Select(g => new RevenuePointDto { Date = g.Key, Amount = g.Sum(o => o.Total) })
                    .ToList();

                if (!days.HasValue || days.Value <= 1)
                    return series.OrderBy(p => p.Date).ToList();

                var start = DateTime.UtcNow.AddDays(-days.Value).Date;
                var end = DateTime.UtcNow.Date;
                var byDate = series.ToDictionary(p => p.Date);
                var filled = new List<RevenuePointDto>();
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    filled.Add(byDate.TryGetValue(d, out var p) ? p : new RevenuePointDto { Date = d, Amount = 0 });
                }

                return filled;
            }
        }
    }

    public class DashboardAnalyticsDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int PendingReviewsCount { get; set; }
        public List<RevenuePointDto> RevenueOverTime { get; set; } = new();
        public List<OrderStatusCountDto> OrdersByStatus { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
        public List<LowStockProductDto> LowStockProducts { get; set; } = new();
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
    }

    public class RevenuePointDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }

    public class OrderStatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
    }

    public class LowStockProductDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class RecentOrderDto
    {
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
