namespace Majal.Sample;

[ValueObject]
public partial class EmployeeAddress
{
    public string City { get; set; }
    public string Country { get; set; }
    public string PostalCode { get; set; }

    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
        yield return Country;
        yield return PostalCode;
    }
}