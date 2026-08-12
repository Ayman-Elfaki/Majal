namespace Majal.FunctionalTests;

public class TranslatableFunctionalTests
{
    [Fact]
    public void Translatable_ImplementsInterface_AndLocaleIsSettable()
    {
        var article = new Article { Id = 1, Title = "Hello", Locale = "en-US" };

        Assert.IsAssignableFrom<ITranslatable<string>>(article);
        Assert.Equal("en-US", article.Locale);
    }
}

[Entity, Translatable<string>]
public partial class Article
{
    // Public constructor for testing
    public Article()
    {
    }

    public string Title { get; set; } = string.Empty;
}