namespace Majal.FunctionalTests;

public class OrdinalFunctionalTests
{
    [Fact]
    public void Ordinal_ImplementsInterface_AndOrdinalIsSettable()
    {
        var step = new Step { Id = 1, Name = "Step 1", Ordinal = 3 };

        Assert.IsAssignableFrom<IOrdinal>(step);
        Assert.Equal(3u, step.Ordinal);
    }
}

[Entity, Ordinal]
public partial class Step
{
    // Public constructor for testing
    public Step()
    {
    }

    public string Name { get; set; } = string.Empty;
}