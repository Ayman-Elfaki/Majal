using System.Globalization;

namespace EShop.Modules.Catalog.Entities;

/// <summary>
/// Per-locale name and description for a <see cref="Product"/>. Uses the explicit generic
/// <c>[Translatable&lt;CultureInfo&gt;]</c> form, contrasting with <see cref="CategoryTranslation"/>'s bare form.
/// </summary>
[Entity, Translatable<CultureInfo>]
public partial class ProductTranslation
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    public static ProductTranslation Create(string name, string description, string locale) =>
        new() { Name = name, Description = description, Locale = CultureInfo.GetCultureInfo(locale) };
}
