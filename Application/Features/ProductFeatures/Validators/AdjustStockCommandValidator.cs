using FluentValidation;

namespace Application.Features.ProductFeatures.Commands
{
    public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
    {
        public AdjustStockCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("Product ID is required.");

            RuleFor(x => x.Quantity)
                .NotEqual(0).WithMessage("Quantity adjustment cannot be zero.");
        }
    }
}
