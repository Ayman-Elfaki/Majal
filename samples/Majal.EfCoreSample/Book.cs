
namespace Majal.EfCoreSample;

[Entity]
[Aggregate]
[Auditable, Archivable]
public partial class Book
{
    public required BookName Name { get; init; }
    public required BookPublishYear PublishYear { get; init; }
    
    public BookCategory? Category { get; set; }

    public List<Author> Authors { get; init; } = [];
    public List<BookTranslation> Translations { get; init; } = [];

    public static Book Create(string name, DateOnly publishYear, IEnumerable<BookTranslation> translations)
    {
        string[] languages = ["en", "de"];

        if (!languages.All(l => translations.Any(t => t.Locale == l)))
            throw new ArgumentException("Translations must contain both 'en' and 'de' languages");

        return new Book
        {
            Name = BookName.Create(name),
            PublishYear = BookPublishYear.Create(publishYear),
            Translations = [.. translations],
        };
    }
}