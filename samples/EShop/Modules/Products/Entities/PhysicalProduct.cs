using EShop.Modules.Categories.Entities;
using EShop.Modules.Products.ValueObjects;

namespace EShop.Modules.Products.Entities;

public class PhysicalProduct : Product
{
    public decimal WeightKg { get; private init; }

    public static PhysicalProduct Create(ProductSku sku, Money price, Category category, ProductTags tags,
        IEnumerable<ProductTranslation> translations, decimal weightKg, uint initialStockQuantity) =>
        new()
        {
            Sku = sku,
            Price = price,
            Category = category,
            TagList = tags,
            Translations = [.. translations],
            WeightKg = weightKg,
            StockQuantity = initialStockQuantity,
            Ordinal = 0
        };
}
