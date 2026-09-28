using FluentValidation;

namespace Application.Features.PromotionFeatures.Commands
{
    public class UpdateCouponCommandValidator : AbstractValidator<UpdateCouponCommand>
    {
        public UpdateCouponCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
            RuleFor(x => x.Code).NotEmpty().MaximumLength(30).WithMessage("Coupon code is required (max 30).");
        }
    }
}
