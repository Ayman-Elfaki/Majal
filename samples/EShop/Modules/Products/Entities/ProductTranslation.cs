using EShop.Modules.Categories.Entities;
using EShop.Modules.Products.ValueObjects;

namespace EShop.Modules.Products.Entities;

/// <summary>
/// Per-locale name and description for a <see cref="Product"/>. Uses the explicit generic
/// <c>[Translatable&lt;CultureInfo&gt;]</c> form, contrasting with <see cref="CategoryTranslation"/>'s bare form.
/// </summary>
[Entity, Translatable]
public partial class ProductTranslation
{
    public required ProductName Name { get; init; }

    public required ProductDescription Description { get; init; }

    public static ProductTranslation Create(string name, string description, string locale) =>
        new()
        {
            Name = ProductName.Create(name),
            Description = ProductDescription.Create(description),
            Locale = locale
        };
}