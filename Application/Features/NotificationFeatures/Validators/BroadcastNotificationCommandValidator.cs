using FluentValidation;

namespace Application.Features.NotificationFeatures.Commands
{
    public class BroadcastNotificationCommandValidator : AbstractValidator<BroadcastNotificationCommand>
    {
        public BroadcastNotificationCommandValidator()
        {
            RuleFor(x => x.Title).NotEmpty().MaximumLength(200).WithMessage("Title is required (max 200).");
            RuleFor(x => x.Type).NotEmpty().WithMessage("Notification type is required.");
        }
    }
}
