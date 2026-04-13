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
public readonly partial struct AddressCity;