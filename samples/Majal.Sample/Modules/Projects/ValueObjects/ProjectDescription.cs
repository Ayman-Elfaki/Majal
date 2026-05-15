namespace Majal.Sample.Modules.Projects.ValueObjects;
/// <summary>
/// The project name value object
/// </summary>
[ValueObject<string>]
public readonly partial struct ProjectDescription
{
    internal const int MaxLength = 2048;
}