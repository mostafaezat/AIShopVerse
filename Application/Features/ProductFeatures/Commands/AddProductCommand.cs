using Domain.Entities.CatalogEntities;
using Application.Features.ProductFeatures.Queries;

namespace Application.Features.ProductFeatures.Commands
{
    public class AddProductCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameAR { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameEN { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public int? LowStockThreshold { get; set; }

        public string? DescriptionAR { get; set; }
        public string? DescriptionEN { get; set; }

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string CategoryId { get; set; } = string.Empty;

        public string? BrandId { get; set; }
        public List<string>? ImageUrls { get; set; } = new();
        public List<ProductVariantDto>? Variants { get; set; }

        public class AddProductCommandHandler : IRequestHandler<AddProductCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public AddProductCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(AddProductCommand request, CancellationToken cancellationToken)
            {
                var product = new Product
                {
                    NameAR = request.NameAR,
                    NameEN = request.NameEN,
                    SKU = request.SKU,
                    Price = request.Price,
                    DiscountPrice = request.DiscountPrice,
                    StockQuantity = request.StockQuantity,
                    LowStockThreshold = request.LowStockThreshold,
                    DescriptionAR = request.DescriptionAR,
                    DescriptionEN = request.DescriptionEN,
                    CategoryId = request.CategoryId,
                    BrandId = request.BrandId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.Repository<Product>().Create(product);

                if (request.ImageUrls != null && request.ImageUrls.Any())
                {
                    int order = 1;
                    foreach (var url in request.ImageUrls)
                    {
                        var image = new ProductImage
                        {
                            ImageUrl = url,
                            DisplayOrder = order,
                            IsPrimary = order == 1,
                            ProductId = product.Id
                        };
                        _unitOfWork.Repository<ProductImage>().Create(image);
                        order++;
                    }
                }

                if (request.Variants != null && request.Variants.Any())
                {
                    var variants = request.Variants
                        .Where(v => v != null)
                        .ToList();

                    if (variants.Any(v => string.IsNullOrWhiteSpace(v.SKU)))
                        return Result<string>.Falid(null, "Variant SKU is required.");

                    if (variants.Any(v => string.IsNullOrWhiteSpace(v.Size) || string.IsNullOrWhiteSpace(v.Color)))
                        return Result<string>.Falid(null, "Variant size and color are required.");

                    var duplicateSku = variants
                        .GroupBy(v => v.SKU.Trim(), StringComparer.OrdinalIgnoreCase)
                        .FirstOrDefault(g => g.Count() > 1);
                    if (duplicateSku != null)
                        return Result<string>.Falid(null, $"Variant SKU '{duplicateSku.Key}' is duplicated.");

                    var duplicateOption = variants
                        .GroupBy(v => (v.Size.Trim().ToLowerInvariant(), v.Color.Trim().ToLowerInvariant()))
                        .FirstOrDefault(g => g.Count() > 1);
                    if (duplicateOption != null)
                        return Result<string>.Falid(null, "Variant size/color combination is duplicated.");

                    var requestedSkus = variants.Select(v => v.SKU.Trim().ToLowerInvariant()).ToArray();

                    var existingSku = await _unitOfWork.Repository<ProductVariant>()
                        .FindByCondition(v => requestedSkus.Contains(v.SKU.ToLower()))
                        .AnyAsync(cancellationToken);
                    if (existingSku)
                        return Result<string>.Falid(null, "A variant SKU already exists.");

                    foreach (var v in variants)
                    {
                        var variant = new ProductVariant
                        {
                            ProductId = product.Id,
                            SKU = v.SKU.Trim(),
                            Price = v.Price,
                            StockQuantity = v.StockQuantity,
                            Size = v.Size.Trim(),
                            Color = v.Color.Trim(),
                            IsActive = true
                        };
                        _unitOfWork.Repository<ProductVariant>().Create(variant);
                    }
                }

                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(product.Id, ResourcesLocalizationKeys.AddSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.AddFailed);
            }
        }
    }
}
