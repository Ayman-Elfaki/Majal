namespace EShop.Modules.Catalog.ValueObjects;

[ValueObject]
public readonly partial struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public static Money Create(decimal amount, string currency) =>
        new() { Amount = amount, Currency = currency };

    private IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
