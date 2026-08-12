namespace Majal.FunctionalTests;

public class AuditableFunctionalTests
{
    [Fact]
    public void Auditable_ImplementsInterface_AndTimestampsAreSettable()
    {
        var comment = new Comment { Id = 1, Text = "Hello" };
        var createdOn = DateTimeOffset.UtcNow;
        var updatedOn = createdOn.AddMinutes(5);

        Assert.IsAssignableFrom<IAuditable>(comment);

        comment.CreatedOn = createdOn;
        comment.UpdatedOn = updatedOn;

        Assert.Equal(createdOn, comment.CreatedOn);
        Assert.Equal(updatedOn, comment.UpdatedOn);
    }

    [Fact]
    public void Auditable_UpdatedOn_DefaultsToNull()
    {
        var comment = new Comment { Id = 1, Text = "Hello" };

        Assert.Null(comment.UpdatedOn);
    }
}

[Entity, Auditable]
public partial class Comment
{
    // Public constructor for testing
    public Comment()
    {
    }

    public string Text { get; set; } = string.Empty;
}