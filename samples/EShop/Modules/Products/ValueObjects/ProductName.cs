namespace EShop.Modules.Products.ValueObjects;

[ValueObject<string>]
public readonly partial struct ProductName
{
    public const int MaxLength = 100;
}