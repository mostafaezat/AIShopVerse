using Domain.Entities.CatalogEntities;
using Domain.Entities.OrderEntities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services
{
    /// <summary>
    /// Restores order-line stock only when the order actually has stock deducted
    /// (StockDeducted flag), then clears the flag so restoration is idempotent.
    /// Never restores for never-deducted (e.g. unpaid card) orders.
    /// </summary>
    public interface IStockRestorer
    {
        Task RestoreAsync(Order order, CancellationToken cancellationToken = default);
    }

    public class StockRestorer : IStockRestorer
    {
        private readonly IUnitOfWork _unitOfWork;

        public StockRestorer(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task RestoreAsync(Order order, CancellationToken cancellationToken = default)
        {
            if (!order.StockDeducted)
                return;

            foreach (var item in order.Items)
            {
                if (!string.IsNullOrEmpty(item.VariantId))
                {
                    var variant = await _unitOfWork.Repository<ProductVariant>()
                        .FindByCondition(v => v.Id == item.VariantId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (variant != null)
                    {
                        variant.StockQuantity += item.Quantity;
                        _unitOfWork.Repository<ProductVariant>().Update(variant);
                    }
                    continue;
                }

                var product = await _unitOfWork.Repository<Product>()
                    .FindByCondition(p => p.Id == item.ProductId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (product != null)
                {
                    product.StockQuantity += item.Quantity;
                    product.LastModifiedAt = DateTime.UtcNow;
                    _unitOfWork.Repository<Product>().Update(product);
                }
            }

            order.StockDeducted = false;
            _unitOfWork.Repository<Order>().Update(order);
        }
    }
}