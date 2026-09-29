using Application.Features.CartFeatures.Queries;
using Application.Services;
using Domain.Entities.CartEntities;
using Domain.Entities.CatalogEntities;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.CartFeatures
{
    internal static class CartProjector
    {
        public static async Task<(List<CartItemDto> Items, decimal Subtotal)> BuildItemsAsync(
            IUnitOfWork unitOfWork,
            Cart cart,
            CancellationToken cancellationToken)
        {
            var productIds = cart.Items.Select(i => i.ProductId).Distinct().ToList();

            var products = productIds.Count == 0
                ? new List<Product>()
                : await unitOfWork.Repository<Product>()
                    .FindByCondition(p => productIds.Contains(p.Id))
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

            var productMap = products.ToDictionary(p => p.Id);

            var items = new List<CartItemDto>(cart.Items.Count);
            decimal subtotal = 0;

            foreach (var item in cart.Items)
            {
                productMap.TryGetValue(item.ProductId, out var product);

                string? variantLabel = null;
                decimal currentPrice;

                if (!string.IsNullOrEmpty(item.VariantId) && product != null)
                {
                    var variant = product.Variants.FirstOrDefault(v => v.Id == item.VariantId);
                    currentPrice = variant?.Price ?? item.UnitPrice;
                    variantLabel = ProductVariantLabels.For(variant);
                }
                else
                {
                    currentPrice = product?.DiscountPrice ?? product?.Price ?? item.UnitPrice;
                }

                item.UnitPrice = currentPrice;

                var itemTotal = currentPrice * item.Quantity;
                subtotal += itemTotal;

                items.Add(new CartItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    VariantLabel = variantLabel,
                    ProductName = product?.NameEN ?? "",
                    ProductImageUrl = product?.Images.FirstOrDefault(i => i.IsPrimary)?.ImageUrl,
                    Quantity = item.Quantity,
                    UnitPrice = currentPrice,
                    TotalPrice = itemTotal
                });
            }

            return (items, subtotal);
        }
    }
}