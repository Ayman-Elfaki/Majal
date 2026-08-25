using EShop.Modules.Categories.Entities;
using EShop.Modules.Products.ValueObjects;

namespace EShop.Modules.Products.Entities;

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
            Translations = [.. translations],
            DownloadUrl = downloadUrl,
            StockQuantity = initialStockQuantity,
            Ordinal = 0
        };
}
