using FluentValidation;

namespace Application.Features.PromotionFeatures.Commands
{
    public class DeleteCouponCommandValidator : AbstractValidator<DeleteCouponCommand>
    {
        public DeleteCouponCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        }
    }
}
