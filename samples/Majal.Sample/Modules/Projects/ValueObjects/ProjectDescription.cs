namespace Majal.Sample.Modules.Projects.ValueObjects;

/// <summary>
/// The project name value object
/// </summary>
[ValueObject<string>]
public readonly partial struct ProjectDescription
{
    internal const int MaxLength = 2048;
}

[ValueObject]
public partial class Capacity
{
    /// <summary>
    /// Maximum capacity
    /// </summary>
    public required uint Maximum { get; init; }

    /// <summary>
    /// Minimum capacity
    /// </summary>
    public required uint Minimum { get; init; }

    /// <summary>
    /// Create Capacity
    /// </summary>
    /// <param name="maximum"></param>
    /// <param name="minimum"></param>
    /// <returns></returns>
    public static Capacity Create(uint maximum, uint minimum)
    {
        return new Capacity
        {
            Maximum = maximum,
            Minimum = minimum
        };
    }

    private IEnumerable<object?> GetEqualityComponents()
    {
        yield return Maximum;
        yield return Minimum;
    }
}