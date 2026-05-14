namespace Majal.Sample.Modules.Projects.ValueObjects;

[ValueObject<string>]
public readonly partial struct ProjectName
{
    public const int MaxLength = 200;
}

[ValueObject]
public readonly partial struct ProjectTags
{
    public List<string> Values { get; init; }

    public static ProjectTags Create(HashSet<string> values)
    {
        return new ProjectTags
        {
            Values = [..values]
        };
    }

    private IEnumerable<object> GetEqualityComponents()
    {
        yield return Values;
    }
}