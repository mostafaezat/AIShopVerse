using Domain.Entities.CatalogEntities;

namespace Application.Features.ProductFeatures.Queries
{
    public class GetAllProductsQuery : IRequest<Result<PaginatedResult<ProductListItemDto>>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 24;
        public string? SearchTerm { get; set; }

        public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, Result<PaginatedResult<ProductListItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetAllProductsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<PaginatedResult<ProductListItemDto>>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
            {
                var query = _unitOfWork.Repository<Product>().FindAll();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(p =>
                        p.NameEN.Contains(request.SearchTerm) ||
                        p.NameAR.Contains(request.SearchTerm) ||
                        p.SKU.Contains(request.SearchTerm));
                }

                var totalItems = await query.CountAsync(cancellationToken);

                var items = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new ProductListItemDto
                    {
                        Id = p.Id,
                        NameAR = p.NameAR,
                        NameEN = p.NameEN,
                        SKU = p.SKU,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        StockQuantity = p.StockQuantity,
                        IsActive = p.IsActive,
                        HasVariants = p.Variants.Any(v => v.IsActive),
                        CategoryName = p.Category.NameEN,
                        BrandName = p.Brand != null ? p.Brand.NameEN : null,
                        PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
                        AverageRating = p.Reviews.Where(r => r.IsApproved).Any() ? p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0,
                        ReviewCount = p.Reviews.Count(r => r.IsApproved),
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                var paginatedResult = PaginatedResult<ProductListItemDto>.Create(items, totalItems, request.Page, request.PageSize);
                return Result<PaginatedResult<ProductListItemDto>>.Success(paginatedResult);
            }
        }
    }

    public class GetFilteredProductsQuery : IRequest<Result<PaginatedResult<ProductListItemDto>>>
    {
        public string? CategoryId { get; set; }
        public string? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinRating { get; set; }
        public bool InStockOnly { get; set; }
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 24;

        public class GetFilteredProductsQueryHandler : IRequestHandler<GetFilteredProductsQuery, Result<PaginatedResult<ProductListItemDto>>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetFilteredProductsQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<PaginatedResult<ProductListItemDto>>> Handle(GetFilteredProductsQuery request, CancellationToken cancellationToken)
            {
                var query = _unitOfWork.Repository<Product>().FindAll()
                    .Where(p => p.IsActive);

                if (!string.IsNullOrEmpty(request.CategoryId))
                    query = query.Where(p => p.CategoryId == request.CategoryId);

                if (!string.IsNullOrEmpty(request.BrandId))
                    query = query.Where(p => p.BrandId == request.BrandId);

                if (request.MinPrice.HasValue)
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) >= request.MinPrice);

                if (request.MaxPrice.HasValue)
                    query = query.Where(p => (p.DiscountPrice ?? p.Price) <= request.MaxPrice);

                if (request.InStockOnly)
                    query = query.Where(p => p.StockQuantity > 0);

                var searchTerm = request.SearchTerm?.Trim();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(p =>
                        p.NameEN.Contains(searchTerm) ||
                        p.NameAR.Contains(searchTerm) ||
                        p.SKU.Contains(searchTerm) ||
                        p.Category.NameEN.Contains(searchTerm) ||
                        (p.Brand != null && p.Brand.NameEN.Contains(searchTerm)) ||
                        (p.DescriptionEN != null && p.DescriptionEN.Contains(searchTerm)) ||
                        (p.DescriptionAR != null && p.DescriptionAR.Contains(searchTerm)));
                }

                if (request.MinRating.HasValue)
                {
                    query = query.Where(p => p.Reviews.Any(r => r.IsApproved) &&
                        p.Reviews.Where(r => r.IsApproved).Average(r => (double?)r.Rating) >= request.MinRating.Value);
                }

                var useRelevance = !string.IsNullOrEmpty(searchTerm) &&
                    (string.IsNullOrEmpty(request.SortBy) || request.SortBy == "newest" || request.SortBy == "relevance");

                if (useRelevance)
                {
                    var term = searchTerm!;
                    query = query
                        .OrderByDescending(p => p.NameEN.StartsWith(term))
                        .ThenByDescending(p => p.NameAR.StartsWith(term))
                        .ThenByDescending(p => p.NameEN.Contains(term) || p.NameAR.Contains(term))
                        .ThenByDescending(p => p.SKU.Contains(term))
                        .ThenByDescending(p => p.Category.NameEN.Contains(term) ||
                            (p.Brand != null && p.Brand.NameEN.Contains(term)) ||
                            (p.DescriptionEN != null && p.DescriptionEN.Contains(term)) ||
                            (p.DescriptionAR != null && p.DescriptionAR.Contains(term)))
                        .ThenByDescending(p => p.CreatedAt);
                }
                else
                {
                    query = request.SortBy switch
                    {
                        "price_asc" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
                        "price_desc" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
                        "newest" => query.OrderByDescending(p => p.CreatedAt),
                        "rating" => query.OrderByDescending(p => p.Reviews.Where(r => r.IsApproved).Any()
                            ? p.Reviews.Where(r => r.IsApproved).Average(r => (double?)r.Rating) : 0),
                        _ => query.OrderByDescending(p => p.CreatedAt)
                    };
                }

                var totalItems = await query.CountAsync(cancellationToken);

                var items = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new ProductListItemDto
                    {
                        Id = p.Id,
                        NameAR = p.NameAR,
                        NameEN = p.NameEN,
                        SKU = p.SKU,
                        Price = p.Price,
                        DiscountPrice = p.DiscountPrice,
                        StockQuantity = p.StockQuantity,
                        IsActive = p.IsActive,
                        HasVariants = p.Variants.Any(v => v.IsActive),
                        CategoryName = p.Category.NameEN,
                        BrandName = p.Brand != null ? p.Brand.NameEN : null,
                        PrimaryImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.ImageUrl).FirstOrDefault(),
                        AverageRating = p.Reviews.Where(r => r.IsApproved).Any() ? p.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0,
                        ReviewCount = p.Reviews.Count(r => r.IsApproved),
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                var paginatedResult = PaginatedResult<ProductListItemDto>.Create(items, totalItems, request.Page, request.PageSize);
                return Result<PaginatedResult<ProductListItemDto>>.Success(paginatedResult);
            }
        }
    }

    public class GetProductByIdQuery : IRequest<Result<ProductDetailDto>>
    {
        public string Id { get; set; } = string.Empty;

        public GetProductByIdQuery(string id) { Id = id; }

        public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, Result<ProductDetailDto>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public GetProductByIdQueryHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<ProductDetailDto>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
            {
                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == request.Id && p.IsActive)
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Images)
                    .Include(p => p.Attributes)
                    .Include(p => p.Variants)
                    .Include(p => p.Reviews)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    return Result<ProductDetailDto>.Falid(null, ResourcesLocalizationKeys.NotFound);

                var dto = new ProductDetailDto
                {
                    Id = product.Id,
                    NameAR = product.NameAR,
                    NameEN = product.NameEN,
                    SKU = product.SKU,
                    Price = product.Price,
                    DiscountPrice = product.DiscountPrice,
                    StockQuantity = product.StockQuantity,
                    LowStockThreshold = product.LowStockThreshold,
                    DescriptionAR = product.DescriptionAR,
                    DescriptionEN = product.DescriptionEN,
                    IsActive = product.IsActive,
                    CategoryId = product.CategoryId,
                    CategoryName = product.Category.NameEN,
                    BrandId = product.BrandId,
                    BrandName = product.Brand?.NameEN,
                    Images = product.Images.Select(i => new ProductImageDto
                    {
                        Id = i.Id,
                        ImageUrl = i.ImageUrl,
                        DisplayOrder = i.DisplayOrder,
                        IsPrimary = i.IsPrimary
                    }).ToList(),
                    Attributes = product.Attributes.Select(a => new ProductAttributeDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Value = a.Value
                    }).ToList(),
                    Variants = product.Variants.Select(v => new ProductVariantDto
                    {
                        Id = v.Id,
                        SKU = v.SKU,
                        Price = v.Price,
                        StockQuantity = v.StockQuantity,
                        Size = v.Size,
                        Color = v.Color,
                        IsActive = v.IsActive,
                        AttributeValues = v.AttributeValues
                    }).ToList(),
                    AverageRating = product.Reviews.Where(r => r.IsApproved).Any() ? product.Reviews.Where(r => r.IsApproved).Average(r => r.Rating) : 0,
                    ReviewCount = product.Reviews.Count(r => r.IsApproved),
                    CreatedAt = product.CreatedAt
                };

                return Result<ProductDetailDto>.Success(dto);
            }
        }
    }

    public class ProductListItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool HasVariants { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductDetailDto
    {
        public string Id { get; set; } = string.Empty;
        public string NameAR { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public int? LowStockThreshold { get; set; }
        public string? DescriptionAR { get; set; }
        public string? DescriptionEN { get; set; }
        public bool IsActive { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? BrandId { get; set; }
        public string? BrandName { get; set; }
        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductAttributeDto> Attributes { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductVariantDto
    {
        public string Id { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
        public string Size { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string? AttributeValues { get; set; }
    }

    public class ProductImageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class ProductAttributeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
