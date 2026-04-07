
namespace Majal.Samples;

[ValueObject]
public partial class EmpolyeePhone
{
    public required string Number { get; init; }
    public required string Country { get; init; }

    public static partial EmpolyeePhone Create(string number, string country)
    {
        return new EmpolyeePhone
        {
            Number = number,
            Country = country
        };
    }

    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return Number;
        yield return Country;
    }
}