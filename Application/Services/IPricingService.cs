using Domain.Entities.PromotionEntities;

namespace Application.Services
{
    public interface IPricingService
    {
        PricingSummary ComputeTotals(decimal subtotal, decimal discountAmount);
        Task<CouponValidationResult> ValidateAndApplyCouponAsync(string? couponCode, decimal subtotal, CancellationToken cancellationToken);
    }

    public class PricingService : IPricingService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PricingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public PricingSummary ComputeTotals(decimal subtotal, decimal discountAmount)
        {
            decimal taxableAmount = subtotal - discountAmount;
            if (taxableAmount < 0) taxableAmount = 0;
            decimal tax = Math.Round(taxableAmount * 0.15m, 2);
            decimal shipping = taxableAmount > 100 ? 0 : 10;
            decimal total = Math.Round(taxableAmount + tax + shipping, 2);

            return new PricingSummary
            {
                Subtotal = subtotal,
                DiscountAmount = discountAmount,
                Tax = tax,
                ShippingCost = shipping,
                Total = total
            };
        }

        public async Task<CouponValidationResult> ValidateAndApplyCouponAsync(string? couponCode, decimal subtotal, CancellationToken cancellationToken)
        {
            var result = new CouponValidationResult();

            if (string.IsNullOrWhiteSpace(couponCode))
            {
                result.IsValid = true;
                result.DiscountAmount = 0;
                return result;
            }

            var coupon = await _unitOfWork.Repository<Coupon>()
                .FindByCondition(c => c.Code.ToLower() == couponCode.Trim().ToLower())
                .FirstOrDefaultAsync(cancellationToken);

            if (coupon == null)
            {
                result.Error = $"Coupon '{couponCode}' is not valid.";
                return result;
            }

            if (!coupon.IsActive)
            {
                result.Error = $"Coupon '{coupon.Code}' is no longer active.";
                return result;
            }

            if (DateTime.UtcNow < coupon.ValidFrom)
            {
                result.Error = $"Coupon '{coupon.Code}' is not yet valid.";
                return result;
            }

            if (DateTime.UtcNow > coupon.ValidTo)
            {
                result.Error = $"Coupon '{coupon.Code}' has expired.";
                return result;
            }

            if (coupon.MaxUses > 0 && coupon.UsedCount >= coupon.MaxUses)
            {
                result.Error = $"Coupon '{coupon.Code}' has reached its usage limit.";
                return result;
            }

            if (coupon.MinOrderValue.HasValue && subtotal < coupon.MinOrderValue.Value)
            {
                result.Error = $"Coupon '{coupon.Code}' requires a minimum order of {coupon.MinOrderValue.Value:C}.";
                return result;
            }

            decimal discount = coupon.DiscountType == DiscountType.Percentage
                ? subtotal * (coupon.DiscountValue / 100m)
                : coupon.DiscountValue;

            if (discount > subtotal) discount = subtotal;
            discount = Math.Round(discount, 2);

            result.IsValid = true;
            result.DiscountAmount = discount;
            result.Coupon = coupon;
            return result;
        }
    }

    public class PricingSummary
    {
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Tax { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
    }

    public class CouponValidationResult
    {
        public bool IsValid { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Error { get; set; }
        public Coupon? Coupon { get; set; }
    }
}
