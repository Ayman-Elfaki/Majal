namespace Majal.Sample.Modules.Projects.ValueObjects;

/// <summary>
/// The project name value object
/// </summary>
[ValueObject<string>]
public readonly partial struct ProjectName
{
    internal const int MaxLength = 200;
}