using System.Globalization;

namespace EShop.Modules.Catalog.Entities;

/// <summary>
/// Per-locale description for a <see cref="Category"/>. Uses the bare, non-generic
/// <c>[Translatable]</c> form, relying on the assembly-level <c>DefaultLocaleType</c>.
/// </summary>
[Entity, Translatable]
public partial class CategoryTranslation
{
    public string Description { get; private set; } = string.Empty;

    public static CategoryTranslation Create(string description, string locale) =>
        new() { Description = description, Locale = CultureInfo.GetCultureInfo(locale) };
}
