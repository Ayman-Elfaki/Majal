using EShop.Modules.Catalog.ValueObjects;

namespace EShop.Modules.Catalog.Entities;

public class DigitalProduct : Product
{
    public string DownloadUrl { get; private init; } = string.Empty;

    public static DigitalProduct Create(ProductSku sku, Money price, Category category, ProductTags tags,
        IEnumerable<ProductTranslation> translations, string downloadUrl, uint initialStockQuantity) =>
        new()
        {
            Sku = sku,
            Price = price,
            Category = category,
            TagList = tags,
            TranslationList = [.. translations],
            DownloadUrl = downloadUrl,
            StockQuantity = initialStockQuantity,
            Ordinal = 0
        };
}
