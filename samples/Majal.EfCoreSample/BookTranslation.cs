namespace Majal.EfCoreSample;

[Entity]
[Translatable]
public partial class BookTranslation
{
    public required BookContent Content { get; init; }

    public BookTranslation()
    {
    }
}