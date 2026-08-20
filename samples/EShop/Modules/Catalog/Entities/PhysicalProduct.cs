using EShop.Modules.Catalog.ValueObjects;

namespace EShop.Modules.Catalog.Entities;

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
            TranslationList = [.. translations],
            WeightKg = weightKg,
            StockQuantity = initialStockQuantity,
            Ordinal = 0
        };
}
