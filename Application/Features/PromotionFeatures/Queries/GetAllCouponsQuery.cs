using Domain.Entities.PromotionEntities;

namespace Application.Features.PromotionFeatures.Queries
{
    public class GetAllCouponsQuery : IRequest<Result<List<CouponDto>>>
    {
        public class GetAllCouponsQueryHandler : IRequestHandler<GetAllCouponsQuery, Result<List<CouponDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetAllCouponsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<CouponDto>>> Handle(GetAllCouponsQuery request, CancellationToken cancellationToken)
            {
                var coupons = await _unitOfWork.Repository<Coupon>()
                    .FindAll()
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CouponDto
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Description = c.Description,
                        DiscountType = c.DiscountType,
                        DiscountValue = c.DiscountValue,
                        MinOrderValue = c.MinOrderValue,
                        MaxUses = c.MaxUses,
                        UsedCount = c.UsedCount,
                        ValidFrom = c.ValidFrom,
                        ValidTo = c.ValidTo,
                        IsActive = c.IsActive
                    })
                    .ToListAsync(cancellationToken);

                return Result<List<CouponDto>>.Success(coupons);
            }
        }
    }

    public class ValidateCouponQuery : IRequest<Result<CouponValidationDto>>
    {
        public string Code { get; set; } = string.Empty;
        public decimal OrderTotal { get; set; }

        public class ValidateCouponQueryHandler : IRequestHandler<ValidateCouponQuery, Result<CouponValidationDto>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public ValidateCouponQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<CouponValidationDto>> Handle(ValidateCouponQuery request, CancellationToken cancellationToken)
            {
                var coupon = await _unitOfWork.Repository<Coupon>()
                    .FindByCondition(c => c.Code == request.Code.ToUpper())
                    .FirstOrDefaultAsync(cancellationToken);

                if (coupon == null)
                    return Result<CouponValidationDto>.Falid(null, "Invalid coupon code.");

                if (!coupon.IsValid)
                    return Result<CouponValidationDto>.Falid(null, "Coupon is expired or has reached maximum usage.");

                if (coupon.MinOrderValue.HasValue && request.OrderTotal < coupon.MinOrderValue)
                    return Result<CouponValidationDto>.Falid(null, $"Minimum order value is {coupon.MinOrderValue:C}.");

                decimal discountAmount = coupon.DiscountType == DiscountType.Percentage
                    ? request.OrderTotal * (coupon.DiscountValue / 100)
                    : coupon.DiscountValue;

                return Result<CouponValidationDto>.Success(new CouponValidationDto
                {
                    CouponId = coupon.Id,
                    Code = coupon.Code,
                    DiscountAmount = discountAmount,
                    DiscountType = coupon.DiscountType.ToString(),
                    Message = "Coupon applied successfully."
                });
            }
        }
    }

    public class CouponDto
    {
        public string Id { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DiscountType DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal? MinOrderValue { get; set; }
        public int MaxUses { get; set; }
        public int UsedCount { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; }
    }

    public class CouponValidationDto
    {
        public string CouponId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public string DiscountType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
