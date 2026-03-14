namespace Majal.Sample;

[ValueObject]
public partial class EmployeeAddress
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string PostalCode { get; init; }

    
    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Country;
        yield return PostalCode;
    }

    public static partial EmployeeAddress Create(string city, string country, string postalCode)
    {
        return new EmployeeAddress
        {
            City = city,
            Country = country,
            PostalCode = postalCode
        };
    }
}