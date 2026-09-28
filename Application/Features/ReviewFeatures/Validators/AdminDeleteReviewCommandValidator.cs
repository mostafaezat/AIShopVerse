using FluentValidation;

namespace Application.Features.ReviewFeatures.Commands
{
    public class AdminDeleteReviewCommandValidator : AbstractValidator<AdminDeleteReviewCommand>
    {
        public AdminDeleteReviewCommandValidator()
        {
            RuleFor(x => x.ReviewId).NotEmpty().WithMessage("Review id is required.");
        }
    }
}
