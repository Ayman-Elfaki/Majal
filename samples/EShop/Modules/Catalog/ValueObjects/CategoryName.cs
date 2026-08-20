namespace EShop.Modules.Catalog.ValueObjects;

[ValueObject<string>]
public readonly partial struct CategoryName
{
    public const int MaxLength = 100;
}
