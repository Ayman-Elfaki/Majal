namespace Majal.EfCoreSample;

[Entity]
[Aggregate]
[Auditable, Archivable]
public partial class Book
{
    public required BookName Name { get; init; }
    public List<Author> Authors { get; init; } = [];
    public List<BookTranslation> Translations { get; init; } = [];

    public static Book Create(string name, IEnumerable<BookTranslation> translations)
    {
        string[] languages = ["en", "de"];

        if (!languages.All(l => translations.Any(t => t.Locale == l)))
            throw new ArgumentException("Translations must contain both 'en' and 'de' languages");

        return new Book
        {
            Name = BookName.Create(name),
            Translations = [.. translations],
        };
    }
}