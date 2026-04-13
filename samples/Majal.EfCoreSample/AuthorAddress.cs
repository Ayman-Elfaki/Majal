namespace Majal.EfCoreSample;

[ValueObject]
public readonly partial struct AuthorAddress
{
    public required AddressCity City { get; init; }
    public static partial AuthorAddress Create(AddressCity city)
    {
        return new AuthorAddress
        {
            City = city
        };
    }

    private partial AddressCity GetEqualityComponents()
    {
        return City;
    }
}