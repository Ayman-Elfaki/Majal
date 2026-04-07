namespace Majal.EfCoreSample;

[Entity, Aggregate, Archivable]
public partial class Author
{
    public required AuthorName Name { get; init; }
    public List<Book> Books { get; } = [];

    public static Author Create(string name)
    {
        return new Author
        {
            Name = AuthorName.Create(name)
        };
    }
}