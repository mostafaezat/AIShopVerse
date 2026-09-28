using FluentValidation;

namespace Application.Features.PaymentFeatures.Commands
{
    public class RefundOrderCommandValidator : AbstractValidator<RefundOrderCommand>
    {
        public RefundOrderCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order id is required.");
        }
    }
}
