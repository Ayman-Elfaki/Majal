namespace Majal.Samples;

[ValueObject]
public readonly partial struct EmployeeAddress
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string PostalCode { get; init; }

    public static partial EmployeeAddress Create(string city, string country, string postalCode)
    {
        return new EmployeeAddress
        {
            City = city,
            Country = country,
            PostalCode = postalCode
        };
    }

    private partial (string, string, string) GetEqualityComponents() =>
        (City, Country, PostalCode);
}