namespace Majal.FunctionalTests;

public class ArchivableFunctionalTests
{
    [Fact]
    public void Archivable_ImplementsInterface_AndDefaultsToNotArchived()
    {
        var document = new Document { Id = 1, Title = "Report" };

        Assert.IsAssignableFrom<IArchivable>(document);
        Assert.False(document.IsArchived);
        Assert.Null(document.ArchivedOn);
    }

    [Fact]
    public void Archivable_CanBeMarkedArchived()
    {
        var document = new Document { Id = 1, Title = "Report" };
        var archivedOn = DateTimeOffset.UtcNow;

        document.IsArchived = true;
        document.ArchivedOn = archivedOn;

        Assert.True(document.IsArchived);
        Assert.Equal(archivedOn, document.ArchivedOn);
    }
}

[Entity, Archivable]
public partial class Document
{
    // Public constructor for testing
    public Document()
    {
    }

    public string Title { get; set; } = string.Empty;
}