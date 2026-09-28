using Domain.Entities.ReviewEntities;

namespace Application.Features.ReviewFeatures.Commands
{
    public class ApproveReviewCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ReviewId { get; set; } = string.Empty;

        public class ApproveReviewCommandHandler : IRequestHandler<ApproveReviewCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public ApproveReviewCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(ApproveReviewCommand request, CancellationToken cancellationToken)
            {
                var review = await _unitOfWork.Repository<Review>()
                    .FindByCondition(r => r.Id == request.ReviewId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (review == null)
                    return Result<string>.Falid(null, "Review not found.");

                review.IsApproved = true;
                review.LastModifiedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Review>().Update(review);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, "Review approved.")
                    : Result<string>.Falid(null, "Failed to approve review.");
            }
        }
    }

    public class RejectReviewCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ReviewId { get; set; } = string.Empty;

        public class RejectReviewCommandHandler : IRequestHandler<RejectReviewCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public RejectReviewCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(RejectReviewCommand request, CancellationToken cancellationToken)
            {
                var review = await _unitOfWork.Repository<Review>()
                    .FindByCondition(r => r.Id == request.ReviewId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (review == null)
                    return Result<string>.Falid(null, "Review not found.");

                review.IsApproved = false;
                review.LastModifiedAt = DateTime.UtcNow;
                _unitOfWork.Repository<Review>().Update(review);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, "Review rejected.")
                    : Result<string>.Falid(null, "Failed to reject review.");
            }
        }
    }

    public class AdminDeleteReviewCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ReviewId { get; set; } = string.Empty;

        public class AdminDeleteReviewCommandHandler : IRequestHandler<AdminDeleteReviewCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public AdminDeleteReviewCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(AdminDeleteReviewCommand request, CancellationToken cancellationToken)
            {
                var review = await _unitOfWork.Repository<Review>()
                    .FindByCondition(r => r.Id == request.ReviewId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (review == null)
                    return Result<string>.Falid(null, "Review not found.");

                _unitOfWork.Repository<Review>().Delete(review);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(null, "Review deleted successfully.")
                    : Result<string>.Falid(null, "Failed to delete review.");
            }
        }
    }
}
