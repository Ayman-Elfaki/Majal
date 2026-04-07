
namespace Majal.Samples;

[ValueObject]
public partial class EmpolyeeAddress
{
    public required string City { get; init; }
    public required string Country { get; init; }
    public required string PostalCode { get; init; }

    public static partial EmpolyeeAddress Create(string city, string country, string postalCode)
    {
        return new EmpolyeeAddress
        {
            City = city,
            Country = country,
            PostalCode = postalCode
        };
    }

    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
    
    
}