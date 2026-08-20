using EShop.Modules.Catalog.ValueObjects;

namespace EShop.Modules.Catalog.Entities;

/// <summary>
/// An aggregate root, so products reference it by <c>CategoryId</c> rather than embedding it -- matching
/// the old Todo sample's "look up the aggregate, then call Create" endpoint pattern.
/// </summary>
[Entity, Aggregate]
public partial class Category
{
    public CategoryName Name { get; private set; } = default!;

    /// <summary>
    /// Named differently from the "translations" factory parameter on purpose: <see cref="CategoryTranslation"/>
    /// is <c>[Translatable]</c>, so its own generated FromEntity() needs a supplied "locale" argument the
    /// generator can't thread through a nested-collection forwarding call. This naming routes the whole
    /// collection through the safe "supplied argument" fallback instead of generating invalid code.
    /// </summary>
    public List<CategoryTranslation> TranslationList { get; private set; } = [];

    public static Category Create(CategoryName name, IEnumerable<CategoryTranslation> translations) =>
        new() { Name = name, TranslationList = [.. translations] };
}
