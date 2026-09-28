using Domain.Entities.ReviewEntities;

namespace Application.Features.ReviewFeatures.Queries
{
    public class GetProductReviewsQuery : IRequest<Result<List<ReviewDto>>>
    {
        public string ProductId { get; set; } = string.Empty;

        public class GetProductReviewsQueryHandler : IRequestHandler<GetProductReviewsQuery, Result<List<ReviewDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetProductReviewsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<ReviewDto>>> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
            {
                var reviews = await _unitOfWork.Repository<Review>()
                    .FindByCondition(r => r.ProductId == request.ProductId && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
                var users = await _unitOfWork.Repository<ApplicationUser>()
                    .FindByCondition(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName, u.UserName, u.Email })
                    .ToListAsync(cancellationToken);

                var userMap = users.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName ?? u.Email ?? "");

                var result = reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = userMap.TryGetValue(r.UserId, out var name) ? name : "Anonymous",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList();

                return Result<List<ReviewDto>>.Success(result);
            }
        }
    }

    public class ReviewDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = "Anonymous";
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
