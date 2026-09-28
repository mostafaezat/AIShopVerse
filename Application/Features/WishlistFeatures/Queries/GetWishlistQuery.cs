using Domain.Entities.WishlistEntities;
using Domain.Entities.CatalogEntities;

namespace Application.Features.WishlistFeatures.Queries
{
    public class GetWishlistQuery : IRequest<Result<List<WishlistItemDto>>>
    {
        public class GetWishlistQueryHandler : IRequestHandler<GetWishlistQuery, Result<List<WishlistItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ICurrentUserService _currentUserService;

            public GetWishlistQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
            {
                _unitOfWork = unitOfWork;
                _currentUserService = currentUserService;
            }

            public async Task<Result<List<WishlistItemDto>>> Handle(GetWishlistQuery request, CancellationToken cancellationToken)
            {
                if (string.IsNullOrEmpty(_currentUserService.UserId))
                    return Result<List<WishlistItemDto>>.Falid(null, "User not authenticated.");

                var items = await _unitOfWork.Repository<WishlistItem>()
                    .FindByCondition(w => w.UserId == _currentUserService.UserId)
                    .ToListAsync(cancellationToken);

                var result = new List<WishlistItemDto>();
                foreach (var item in items)
                {
                    var product = await _unitOfWork.Repository<Product>()
                        .FindByCondition(p => p.Id == item.ProductId)
                        .Include(p => p.Images)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (product != null)
                    {
                        result.Add(new WishlistItemDto
                        {
                            Id = item.Id,
                            ProductId = item.ProductId,
                            ProductName = product.NameEN,
                            ProductImageUrl = product.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                            Price = product.DiscountPrice ?? product.Price,
                            AddedAt = item.AddedAt
                        });
                    }
                }

                return Result<List<WishlistItemDto>>.Success(result);
            }
        }
    }

    public class WishlistItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public decimal Price { get; set; }
        public DateTime AddedAt { get; set; }
    }
}
