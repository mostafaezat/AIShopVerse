using FluentValidation;

namespace Application.Features.OrderFeatures.Commands
{
    public class UpdateOrderStatusCommandValidator : AbstractValidator<UpdateOrderStatusCommand>
    {
        public UpdateOrderStatusCommandValidator()
        {
            RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order id is required.");
            RuleFor(x => x.NewStatus).IsInEnum().WithMessage("Invalid order status.");
        }
    }
}
