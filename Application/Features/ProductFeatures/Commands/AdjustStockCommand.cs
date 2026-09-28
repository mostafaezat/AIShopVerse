using Domain.Entities.CatalogEntities;
using Application.Features.InventoryFeatures;

namespace Application.Features.ProductFeatures.Commands
{
    public class AdjustStockCommand : IRequest<Result<int>>
    {
        [Required(ErrorMessage = ErrorMessages.Invalid)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        public string? Reason { get; set; }

        public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, Result<int>>
        {
            private readonly IUnitOfWork _unitOfWork;
            private readonly ILowStockAlerter _lowStockAlerter;

            public AdjustStockCommandHandler(IUnitOfWork unitOfWork, ILowStockAlerter lowStockAlerter)
            {
                _unitOfWork = unitOfWork;
                _lowStockAlerter = lowStockAlerter;
            }

            public async Task<Result<int>> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
            {
                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == request.ProductId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product == null)
                    return Result<int>.Falid(0, ResourcesLocalizationKeys.NotFound);

                var previous = product.StockQuantity;

                product.StockQuantity += request.Quantity;
                if (product.StockQuantity < 0)
                    product.StockQuantity = 0;

                product.LastModifiedAt = DateTime.UtcNow;

                _unitOfWork.Repository<Product>().Update(product);
                await _lowStockAlerter.EvaluateAsync(product, previous, cancellationToken);
                var result = await _unitOfWork.CompleteAsync(cancellationToken);

                return result > 0
                    ? Result<int>.Success(product.StockQuantity, ResourcesLocalizationKeys.UpdateSuccess)
                    : Result<int>.Falid(0, ResourcesLocalizationKeys.UpdateFailed);
            }
        }
    }
}
