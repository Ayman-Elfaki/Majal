namespace Majal.Sample.Modules.Projects.ValueObjects;

[ValueObject<string>]
public readonly partial struct ProjectName
{
    public const int MaxLength = 200;
}