namespace EShop.Modules.Customers.ValueObjects;

[ValueObject<string>]
public readonly partial struct CustomerEmail
{
    public const int MaxLength = 256;
}
