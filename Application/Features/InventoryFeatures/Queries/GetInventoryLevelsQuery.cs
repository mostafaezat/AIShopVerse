using Domain.Entities.CatalogEntities;

namespace Application.Features.InventoryFeatures.Queries
{
    public class GetInventoryLevelsQuery : IRequest<Result<PaginatedResult<InventoryLevelDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public bool? LowStockOnly { get; set; }
        public int LowStockThreshold { get; set; }

        public class GetInventoryLevelsQueryHandler : IRequestHandler<GetInventoryLevelsQuery, Result<PaginatedResult<InventoryLevelDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetInventoryLevelsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<PaginatedResult<InventoryLevelDto>>> Handle(GetInventoryLevelsQuery request, CancellationToken cancellationToken)
            {
                var query = _unitOfWork.Repository<Product>().FindAll();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(p =>
                        p.NameEN.Contains(request.SearchTerm) ||
                        p.NameAR.Contains(request.SearchTerm) ||
                        p.SKU.Contains(request.SearchTerm));
                }

                if (request.LowStockOnly == true && request.LowStockThreshold > 0)
                {
                    query = query.Where(p => p.StockQuantity <= (p.LowStockThreshold ?? request.LowStockThreshold));
                }

                var totalItems = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderBy(p => p.StockQuantity)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new InventoryLevelDto
                    {
                        ProductId = p.Id,
                        NameEN = p.NameEN,
                        NameAR = p.NameAR,
                        SKU = p.SKU,
                        StockQuantity = p.StockQuantity,
                        IsActive = p.IsActive,
                        CategoryName = p.Category.NameEN
                    })
                    .ToListAsync(cancellationToken);

                var paginatedResult = PaginatedResult<InventoryLevelDto>.Create(items, totalItems, request.Page, request.PageSize);
                return Result<PaginatedResult<InventoryLevelDto>>.Success(paginatedResult);
            }
        }
    }

    public class InventoryLevelDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public string? CategoryName { get; set; }
    }
}
