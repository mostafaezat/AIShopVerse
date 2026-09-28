using Domain.Entities.OrderEntities;
using Domain.Entities.PaymentEntities;

namespace Application.Features.OrderFeatures.Queries
{
    public class GetAdminOrderByIdQuery : IRequest<Result<AdminOrderDetailDto>>
    {
        public string OrderId { get; set; } = string.Empty;

        public class GetAdminOrderByIdQueryHandler : IRequestHandler<GetAdminOrderByIdQuery, Result<AdminOrderDetailDto>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetAdminOrderByIdQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<AdminOrderDetailDto>> Handle(GetAdminOrderByIdQuery request, CancellationToken cancellationToken)
            {
                var order = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.Id == request.OrderId)
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(cancellationToken);

                if (order == null)
                    return Result<AdminOrderDetailDto>.Falid(null, "Order not found.");

                var payment = await _unitOfWork.Repository<Payment>()
                    .FindByCondition(p => p.OrderId == order.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                var customer = await _unitOfWork.Repository<ApplicationUser>()
                    .FindByCondition(u => u.Id == order.UserId)
                    .Select(u => new AdminOrderCustomerDto
                    {
                        FullName = u.FullName,
                        UserName = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                return Result<AdminOrderDetailDto>.Success(new AdminOrderDetailDto
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
                    BillingAddress = order.BillingAddress,
                    CouponCode = order.CouponCode,
                    CreatedAt = order.CreatedAt,
                    Customer = customer,
                    Payment = payment == null ? null : new AdminOrderPaymentDto
                    {
                        Amount = payment.Amount,
                        Method = payment.Method,
                        TransactionId = payment.TransactionId,
                        Status = payment.Status.ToString(),
                        PaidAt = payment.PaidAt,
                        GatewayResponse = payment.GatewayResponse
                    },
                    Items = order.Items.Select(i => new AdminOrderItemDto
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
                });
            }
        }
    }

    public class AdminOrderDetailDto
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
        public string? BillingAddress { get; set; }
        public string? CouponCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public AdminOrderCustomerDto? Customer { get; set; }
        public AdminOrderPaymentDto? Payment { get; set; }
        public List<AdminOrderItemDto> Items { get; set; } = new();
    }

    public class AdminOrderCustomerDto
    {
        public string? FullName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class AdminOrderPaymentDto
    {
        public decimal Amount { get; set; }
        public string? Method { get; set; }
        public string? TransactionId { get; set; }
        public string? Status { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? GatewayResponse { get; set; }
    }

    public class AdminOrderItemDto
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