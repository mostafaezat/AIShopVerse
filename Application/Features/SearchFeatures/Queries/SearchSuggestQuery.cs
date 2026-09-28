using Domain.Entities.CatalogEntities;

namespace Application.Features.SearchFeatures.Queries
{
    public class SearchSuggestQuery : IRequest<Result<List<SearchSuggestionDto>>>
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int Count { get; set; } = 8;

        public class SearchSuggestQueryHandler : IRequestHandler<SearchSuggestQuery, Result<List<SearchSuggestionDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public SearchSuggestQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<List<SearchSuggestionDto>>> Handle(SearchSuggestQuery request, CancellationToken cancellationToken)
            {
                var term = request.SearchTerm?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(term))
                    return Result<List<SearchSuggestionDto>>.Success(new List<SearchSuggestionDto>());

                var count = Math.Max(1, Math.Min(20, request.Count));

                var matches = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.IsActive &&
                        (p.NameEN.Contains(term) ||
                         p.NameAR.Contains(term) ||
                         p.SKU.Contains(term)))
                    .OrderByDescending(p => p.NameEN.StartsWith(term))
                    .ThenByDescending(p => p.NameAR.StartsWith(term))
                    .ThenByDescending(p => p.NameEN.Contains(term) || p.NameAR.Contains(term))
                    .ThenByDescending(p => p.CreatedAt)
                    .Take(count)
                    .Select(p => new SearchSuggestionDto
                    {
                        Id = p.Id,
                        NameEN = p.NameEN,
                        NameAR = p.NameAR,
                        SKU = p.SKU,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        ImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault()
                    })
                    .ToListAsync(cancellationToken);

                return Result<List<SearchSuggestionDto>>.Success(matches);
            }
        }
    }

    public class SearchSuggestionDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public string? ImageUrl { get; set; }
    }
}
