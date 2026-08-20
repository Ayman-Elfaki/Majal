namespace EShop.Modules.Catalog.ValueObjects;

[ValueObject]
public readonly partial struct ProductTags
{
    public IReadOnlyList<string> Values { get; init; }

    public static ProductTags Create(IEnumerable<string> values) => new() { Values = [.. values] };

    private IEnumerable<object> GetEqualityComponents()
    {
        yield return Values;
    }
}
