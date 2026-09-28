using Domain.Entities.ReviewEntities;
using Domain.Entities.OrderEntities;

namespace Application.Features.ReviewFeatures.Commands
{
    public class CreateReviewCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ProductId { get; set; } = string.Empty;

        [Range(1, 5, ErrorMessage = ErrorMessages.Invalid)]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public CreateReviewCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<string>> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                // Check if user has a Delivered order containing this product
                var hasDeliveredOrder = await _unitOfWork.Repository<Order>()
                    .FindByCondition(o => o.UserId == _currentUserService.UserId && o.Status == OrderStatus.Delivered)
                    .SelectMany(o => o.Items)
                    .AnyAsync(i => i.ProductId == request.ProductId, cancellationToken);

                if (!hasDeliveredOrder)
                    return Result<string>.Falid(null, "You can only review products from delivered orders.");

                // Check for duplicate review
                var existingReview = await _unitOfWork.Repository<Review>()
                    .FindByCondition(r => r.UserId == _currentUserService.UserId && r.ProductId == request.ProductId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingReview != null)
                    return Result<string>.Falid(null, "You have already reviewed this product.");

                var review = new Review
                {
                    UserId = _currentUserService.UserId,
                    ProductId = request.ProductId,
                    Rating = request.Rating,
                    Comment = request.Comment,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId
                };

                _unitOfWork.Repository<Review>().Create(review);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(review.Id, "Review submitted successfully.")
                    : Result<string>.Falid(null, "Failed to submit review.");
            }
        }
    }

    public class DeleteReviewCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ReviewId { get; set; } = string.Empty;

        public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public DeleteReviewCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<string>> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<string>.Falid(null, "User not authenticated.");

                var review = await _unitOfWork.Repository<Review>()
                    .FindByCondition(r => r.Id == request.ReviewId && r.UserId == _currentUserService.UserId)
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
