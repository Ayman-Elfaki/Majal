namespace EShop.Modules.Customers.ValueObjects;

[ValueObject<string>]
public readonly partial struct CustomerName
{
    public const int MaxLength = 200;
}
