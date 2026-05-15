namespace Majal.Sample.Modules.Issues.Entities;

/// <summary>
/// Resolved Issue entity
/// </summary>
public class ResolvedIssue : Issue
{
    /// <summary>
    /// The date and time the issue was resolved
    /// </summary>
    public required DateTimeOffset ResolvedOn { get; set; }


    /// <summary>
    /// Create a resolved issue
    /// </summary>
    /// <param name="issue">The issue to resolve</param>
    /// <param name="resolvedOn">The date and time the issue was resolved</param>
    /// <returns>The created resolved issue</returns>
    public static ResolvedIssue Create(PendingIssue issue, DateTimeOffset resolvedOn)
    {
        return new ResolvedIssue
        {
            Ordinal = 0,
            Title = issue.Title,
            Priority = issue.Priority,
            StoryPoints = issue.StoryPoints,
            ResolvedOn = resolvedOn,
            Project = issue.Project
        };
    }
}