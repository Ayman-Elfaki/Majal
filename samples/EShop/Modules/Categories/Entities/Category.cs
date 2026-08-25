using EShop.Modules.Categories.ValueObjects;

namespace EShop.Modules.Categories.Entities;

/// <summary>
/// An aggregate root, so products reference it by <c>CategoryId</c> rather than embedding it
/// </summary>
[Entity, Aggregate]
public partial class Category
{
    public CategoryName Name { get; private set; } 
    
    public List<CategoryTranslation> Translations { get; private set; } = [];

    public static Category Create(CategoryName name, IEnumerable<CategoryTranslation> translations) =>
        new() { Name = name, Translations = [.. translations] };
}
