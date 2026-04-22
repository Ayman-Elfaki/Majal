namespace Majal.Sample.Modules.Issues.ValueObjects;

[ValueObject<string>]
public readonly partial struct IssueTitle
{
    public const int MaxLength = 200;
}