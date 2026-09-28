using Domain.Entities.ReviewEntities;
using Domain.Entities.CatalogEntities;
using Domain.Entities.Identity;

namespace Application.Features.ReviewFeatures.Queries
{
    public class GetAllReviewsQuery : IRequest<Result<PaginatedResult<AdminReviewDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool? Approved { get; set; }

        public class GetAllReviewsQueryHandler : IRequestHandler<GetAllReviewsQuery, Result<PaginatedResult<AdminReviewDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetAllReviewsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<PaginatedResult<AdminReviewDto>>> Handle(GetAllReviewsQuery request, CancellationToken cancellationToken)
            {
                var query = _unitOfWork.Repository<Review>().FindAll();

                if (request.Approved.HasValue)
                    query = query.Where(r => r.IsApproved == request.Approved.Value);

                var totalItems = await query.CountAsync(cancellationToken);

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync(cancellationToken);

                var productIds = reviews.Select(r => r.ProductId).Distinct().ToList();
                var userIds = reviews.Select(r => r.UserId).Distinct().ToList();

                var products = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => productIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.NameEN })
                    .ToListAsync(cancellationToken);
                var users = await _unitOfWork.Repository<ApplicationUser>()
                    .FindByCondition(u => userIds.Contains(u.Id))
                    .Select(u => new { u.Id, u.FullName, u.UserName, u.Email })
                    .ToListAsync(cancellationToken);

                var productMap = products.ToDictionary(p => p.Id, p => p.NameEN);
                var userMap = users.ToDictionary(u => u.Id, u => u.FullName ?? u.UserName ?? u.Email ?? "");

                var items = reviews.Select(r => new AdminReviewDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    UserName = userMap.TryGetValue(r.UserId, out var un) ? un : "Anonymous",
                    ProductId = r.ProductId,
                    ProductName = productMap.TryGetValue(r.ProductId, out var pn) ? pn : "",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    IsApproved = r.IsApproved,
                    CreatedAt = r.CreatedAt
                }).ToList();

                var paginatedResult = PaginatedResult<AdminReviewDto>.Create(items, totalItems, request.Page, request.PageSize);
                return Result<PaginatedResult<AdminReviewDto>>.Success(paginatedResult);
            }
        }
    }

    public class AdminReviewDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = "Anonymous";
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsApproved { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }
}
