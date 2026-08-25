using EShop.Modules.Categories.ValueObjects;

namespace EShop.Modules.Categories.Entities;

/// <summary>
/// Per-locale description for a <see cref="Category"/>. Uses the bare, non-generic
/// <c>[Translatable]</c> form, relying on the assembly-level <c>DefaultLocaleType</c>.
/// </summary>
[Entity, Translatable]
public partial class CategoryTranslation
{
    public required CategoryDescription Description { get; init; }

    public static CategoryTranslation Create(string description, string locale) =>
        new() { Description = CategoryDescription.Create(description), Locale = locale };
}