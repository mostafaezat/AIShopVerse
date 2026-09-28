using Domain.Entities.OrderEntities;

namespace Application.Features.OrderFeatures.Queries
{
    public class GetOrderHistoryQuery : IRequest<Result<List<OrderDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public class GetOrderHistoryQueryHandler : IRequestHandler<GetOrderHistoryQuery, Result<List<OrderDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public GetOrderHistoryQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<List<OrderDto>>> Handle(GetOrderHistoryQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<List<OrderDto>>.Falid(null, "User not authenticated.");

                var orders = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.UserId == _currentUserService.UserId)
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Subtotal = o.Subtotal,
                        DiscountAmount = o.DiscountAmount,
                        Tax = o.Tax,
                        ShippingCost = o.ShippingCost,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        CouponCode = o.CouponCode,
                        CreatedAt = o.CreatedAt,
                        Items = o.Items.Select(i => new OrderItemDto
                        {
                            ProductId = i.ProductId,
                            VariantId = i.VariantId,
                            VariantLabel = i.VariantLabel,
                            ProductName = i.ProductName,
                            ProductImageUrl = i.ProductImageUrl,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            TotalPrice = i.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync(cancellationToken);

                return Result<List<OrderDto>>.Success(orders);
            }
        }
    }

    public class GetOrderByIdQuery : IRequest<Result<OrderDto>>
    {
        public string OrderId { get; set; } = string.Empty;

        public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public GetOrderByIdQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<OrderDto>.Falid(null, "User not authenticated.");

                var order = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.Id == request.OrderId && o.UserId == _currentUserService.UserId)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order == null)
                    return Result<OrderDto>.Falid(null, "Order not found.");

                return Result<OrderDto>.Success(new OrderDto
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    Subtotal = order.Subtotal,
                    DiscountAmount = order.DiscountAmount,
                    Tax = order.Tax,
                    ShippingCost = order.ShippingCost,
                    Total = order.Total,
                    Status = order.Status.ToString(),
                    ShippingAddress = order.ShippingAddress,
                    CouponCode = order.CouponCode,
                    CreatedAt = order.CreatedAt,
                    Items = order.Items.Select(i => new OrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        ProductImageUrl = i.ProductImageUrl,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.TotalPrice
                    }).ToList()
                });
            }
        }
    }

    public class GetAllOrdersQuery : IRequest<Result<List<OrderDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public OrderStatus? Status { get; set; }
        public string? SearchTerm { get; set; }

        public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Result<List<OrderDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetAllOrdersQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<OrderDto>>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
            {
                var query = _unitOfWork.Repository<Order>().FindAll();

                if (request.Status.HasValue)
                    query = query.Where(o => o.Status == request.Status);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                    query = query.Where(o => o.OrderNumber.Contains(request.SearchTerm));

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Include(o => o.Items)
                    .Select(o => new OrderDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        Subtotal = o.Subtotal,
                        DiscountAmount = o.DiscountAmount,
                        Tax = o.Tax,
                        ShippingCost = o.ShippingCost,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        CouponCode = o.CouponCode,
                        CreatedAt = o.CreatedAt,
                        Items = o.Items.Select(i => new OrderItemDto
                        {
                            ProductId = i.ProductId,
                            VariantId = i.VariantId,
                            VariantLabel = i.VariantLabel,
                            ProductName = i.ProductName,
                            ProductImageUrl = i.ProductImageUrl,
                            Quantity = i.Quantity,
                            UnitPrice = i.UnitPrice,
                            TotalPrice = i.TotalPrice
                        }).ToList()
                    })
                    .ToListAsync(cancellationToken);

                return Result<List<OrderDto>>.Success(orders);
            }
        }
    }

    public class OrderDto
    {
        public string Id { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public string? CouponCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string? VariantId { get; set; }
        public string? VariantLabel { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
