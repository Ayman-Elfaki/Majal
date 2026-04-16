namespace Majal.Sample.Modules.Projects.ValueObjects;

[ValueObject<string>]
public readonly partial struct ProjectDescription
{
    public const int MaxLength = 2048;
}