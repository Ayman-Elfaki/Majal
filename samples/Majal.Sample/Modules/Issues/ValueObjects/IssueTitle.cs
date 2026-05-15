namespace Majal.Sample.Modules.Issues.ValueObjects;

/// <summary>
/// The issue title value object
/// </summary>
[ValueObject<string>]
public readonly partial struct IssueTitle
{
    internal const int MaxLength = 200;
}