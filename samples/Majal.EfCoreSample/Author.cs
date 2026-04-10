namespace Majal.EfCoreSample;

[Entity, Aggregate, Archivable]
public partial class Author
{
    public required AuthorName Name { get; init; }

    public required AuthorAddress Address { get; set; }

    public static Author Create(string name, AuthorAddress address)
    {
        return new Author
        {
            Name = AuthorName.Create(name),
            Address = address
        };
    }
}

[ValueObject<string>]
public partial class AddressCity;

[ValueObject]
public partial class AuthorAddress
{
    public required AddressCity City { get; init; }

    public static partial AuthorAddress Create(AddressCity city)
    {
        return new AuthorAddress
        {
            City = city
        };
    }

    private partial IEnumerable<object?> GetEqualityComponents()
    {
        yield return City;
    }
}