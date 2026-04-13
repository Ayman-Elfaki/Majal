
namespace Majal.Samples;

[ValueObject]
public readonly partial struct EmployeePhone
{
    public required string Number { get; init; }
    public required string Country { get; init; }

    public static partial EmployeePhone Create(string number, string country)
    {
        return new EmployeePhone
        {
            Number = number,
            Country = country
        };
    }

    private partial (string, string) GetEqualityComponents() => (Number, Country);
}