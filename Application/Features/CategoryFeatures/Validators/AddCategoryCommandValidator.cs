using FluentValidation;

namespace Application.Features.CategoryFeatures.Commands
{
    public class AddCategoryCommandValidator : AbstractValidator<AddCategoryCommand>
    {
        public AddCategoryCommandValidator()
        {
            RuleFor(x => x.NameAR).NotEmpty().MaximumLength(100).WithMessage("Arabic name is required (max 100).");
            RuleFor(x => x.NameEN).NotEmpty().MaximumLength(100).WithMessage("English name is required (max 100).");
            RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("Display order cannot be negative.");
        }
    }
}
