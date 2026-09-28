using Domain.Entities.CatalogEntities;
using Application.Features.ProductFeatures.Queries;

namespace Application.Features.ProductFeatures.Commands
{
    public class UpdateProductCommand : IRequest<Result<string>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameAR { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string NameEN { get; set; } = string.Empty;

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public string? DescriptionAR { get; set; }
        public string? DescriptionEN { get; set; }

        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string CategoryId { get; set; } = string.Empty;

        public string? BrandId { get; set; }
        public bool IsActive { get; set; } = true;
        public int? LowStockThreshold { get; set; }
        public List<string>? ImageUrls { get; set; } = new();
        public List<ProductVariantDto>? Variants { get; set; }

        public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result<string>>
        {
            private readonly IUnitOfWork _unitOfWork;

            public UpdateProductCommandHandler(IUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public async Task<Result<string>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
            {
                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == request.Id)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    return Result<string>.Falid(null, ResourcesLocalizationKeys.NotFound);

                product.NameAR = request.NameAR;
                product.NameEN = request.NameEN;
                product.SKU = request.SKU;
                product.Price = request.Price;
                product.DiscountPrice = request.DiscountPrice;
                product.DescriptionAR = request.DescriptionAR;
                product.DescriptionEN = request.DescriptionEN;
                product.CategoryId = request.CategoryId;
                product.BrandId = request.BrandId;
                product.IsActive = request.IsActive;
                product.LowStockThreshold = request.LowStockThreshold;
                product.LastModifiedAt = DateTime.UtcNow;

                if (request.ImageUrls != null)
                {
                    var existingImages = product.Images.ToList();
                    foreach (var img in existingImages)
                    {
                        _unitOfWork.Repository<ProductImage>().Delete(img);
                    }

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

                if (request.Variants != null)
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

                    var requestedSkus = variants
                        .Select(v => v.SKU.Trim().ToLowerInvariant())
                        .ToArray();

                    var skuCollision = await _unitOfWork.Repository<ProductVariant>()
                        .FindByCondition(v => v.ProductId != product.Id && requestedSkus.Contains(v.SKU.ToLower()))
                        .AnyAsync(cancellationToken);
                    if (skuCollision)
                        return Result<string>.Falid(null, "A variant SKU already exists on another product.");

                    var previouslyActive = product.Variants.Where(v => v.IsActive).ToList();

                    foreach (var incoming in variants)
                    {
                        var matched = product.Variants.FirstOrDefault(v =>
                            string.Equals(v.SKU.Trim(), incoming.SKU.Trim(), StringComparison.OrdinalIgnoreCase));

                        if (matched != null)
                        {
                            matched.SKU = incoming.SKU.Trim();
                            matched.Price = incoming.Price;
                            matched.StockQuantity = incoming.StockQuantity;
                            matched.Size = incoming.Size.Trim();
                            matched.Color = incoming.Color.Trim();
                            matched.IsActive = true;
                        }
                        else
                        {
                            var variant = new ProductVariant
                            {
                                ProductId = product.Id,
                                SKU = incoming.SKU.Trim(),
                                Price = incoming.Price,
                                StockQuantity = incoming.StockQuantity,
                                Size = incoming.Size.Trim(),
                                Color = incoming.Color.Trim(),
                                IsActive = true
                            };
                            _unitOfWork.Repository<ProductVariant>().Create(variant);
                        }
                    }

                    foreach (var existing in previouslyActive)
                    {
                        if (!requestedSkus.Contains(existing.SKU.Trim().ToLowerInvariant()))
                            existing.IsActive = false;
                    }

                    var activeNow = product.Variants.Where(v => v.IsActive && v.ProductId == product.Id).ToList();
                    var activeOptions = activeNow
                        .GroupBy(v => (v.Size.Trim().ToLowerInvariant(), v.Color.Trim().ToLowerInvariant()))
                        .FirstOrDefault(g => g.Count() > 1);
                    if (activeOptions != null)
                        return Result<string>.Falid(null, "Variant size/color combination is duplicated.");
                }

                _unitOfWork.Repository<Product>().Update(product);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<string>.Success(product.Id, ResourcesLocalizationKeys.UpdateSuccess)
                    : Result<string>.Falid(null, ResourcesLocalizationKeys.UpdateFailed);
            }
        }
    }
}
