using FluentValidation;

namespace Application.Features.ProductFeatures.Commands
{
    public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Product ID is required.");

            RuleFor(x => x.NameEN)
                .NotEmpty().WithMessage("English name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(x => x.NameAR)
                .NotEmpty().WithMessage("Arabic name is required.")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

            RuleFor(x => x.SKU)
                .NotEmpty().WithMessage("SKU is required.")
                .MaximumLength(50).WithMessage("SKU must not exceed 50 characters.");

            RuleFor(x => x.Price)
                .GreaterThan(0).WithMessage("Price must be greater than zero.");

            RuleFor(x => x.DiscountPrice)
                .GreaterThan(0).WithMessage("Discount price must be greater than zero.")
                .When(x => x.DiscountPrice.HasValue);

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required.");

            RuleForEach(x => x.Variants)
                .ChildRules(v =>
                {
                    v.RuleFor(x => x!.SKU).NotEmpty().WithMessage("Variant SKU is required.").MaximumLength(100);
                    v.RuleFor(x => x!.Size).NotEmpty().WithMessage("Variant size is required.").MaximumLength(50);
                    v.RuleFor(x => x!.Color).NotEmpty().WithMessage("Variant color is required.").MaximumLength(50);
                    v.RuleFor(x => x!.Price).GreaterThan(0).WithMessage("Variant price must be greater than zero.");
                    v.RuleFor(x => x!.StockQuantity).GreaterThanOrEqualTo(0).WithMessage("Variant stock cannot be negative.");
                });
        }
    }
}
