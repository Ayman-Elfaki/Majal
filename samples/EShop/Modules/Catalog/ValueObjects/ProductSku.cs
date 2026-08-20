namespace EShop.Modules.Catalog.ValueObjects;

[ValueObject<string>]
public readonly partial struct ProductSku
{
    public const int MaxLength = 32;
}
