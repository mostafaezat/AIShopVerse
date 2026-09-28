using Application.Features.OrderFeatures.Queries;
using Application.Services;

namespace Application.Features.PaymentFeatures.Commands
{
    public class CompletePaymentCommand : IRequest<Result<OrderDto>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string OrderId { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string PaymentIntentId { get; set; } = string.Empty;

        public class CompletePaymentCommandHandler : IRequestHandler<CompletePaymentCommand, Result<OrderDto>>
        {
            private readonly IPaymentCompletionService _completionService;

            public CompletePaymentCommandHandler(IPaymentCompletionService completionService)
            {
                _completionService = completionService;
            }

            public Task<Result<OrderDto>> Handle(CompletePaymentCommand request, CancellationToken cancellationToken)
                => _completionService.CompleteAsync(request.OrderId, request.PaymentIntentId, cancellationToken);
        }
    }
}
