namespace EShop.Modules.Products.ValueObjects;

[ValueObject<string>]
public readonly partial struct ProductDescription
{
    public const int MaxLength = 2048;
}