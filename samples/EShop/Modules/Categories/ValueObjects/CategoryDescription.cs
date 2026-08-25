namespace EShop.Modules.Categories.ValueObjects;

[ValueObject<string>]
public readonly partial struct CategoryDescription
{
    public const int MaxLength = 2048;
}