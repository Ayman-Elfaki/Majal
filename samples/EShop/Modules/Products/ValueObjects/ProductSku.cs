namespace EShop.Modules.Products.ValueObjects;

[ValueObject<string>]
public readonly partial struct ProductSku
{
    public const int MaxLength = 32;
}
