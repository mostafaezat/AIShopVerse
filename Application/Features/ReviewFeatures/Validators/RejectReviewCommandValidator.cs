using FluentValidation;

namespace Application.Features.ReviewFeatures.Commands
{
    public class RejectReviewCommandValidator : AbstractValidator<RejectReviewCommand>
    {
        public RejectReviewCommandValidator()
        {
            RuleFor(x => x.ReviewId).NotEmpty().WithMessage("Review id is required.");
        }
    }
}
