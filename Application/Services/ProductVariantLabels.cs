using System.Collections.Generic;
using Domain.Entities.CatalogEntities;

namespace Application.Services
{
    public static class ProductVariantLabels
    {
        public static string? For(ProductVariant? variant)
        {
            if (variant == null)
                return null;

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(variant.Size))
                parts.Add(variant.Size.Trim());
            if (!string.IsNullOrWhiteSpace(variant.Color))
                parts.Add(variant.Color.Trim());

            return parts.Count == 0 ? null : string.Join(" / ", parts);
        }

        public static string? For(string? size, string? color)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(size))
                parts.Add(size.Trim());
            if (!string.IsNullOrWhiteSpace(color))
                parts.Add(color.Trim());

            return parts.Count == 0 ? null : string.Join(" / ", parts);
        }
    }
}