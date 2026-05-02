using Majal.Sample.Modules.Issues.ValueObjects;
using Majal.Sample.Modules.Projects.Entities;

namespace Majal.Sample.Modules.Issues.Entities;

[Entity, Aggregate]
[Ordinal, Archivable, Auditable]
public abstract partial class Issue
{
    public required IssueTitle Title { get; set; }
    public required IssuePriority Priority { get; set; }
    public required IssueStoryPoints StoryPoints { get; set; }
    public Project Project { get; set; } = null!;
}

public class PendingIssue : Issue
{
    public static PendingIssue Create(IssueTitle title, IssuePriority priority, IssueStoryPoints storyPoints)
    {
        return new PendingIssue
        {
            Ordinal = 0,
            Title = title,
            Priority = priority,
            StoryPoints = storyPoints,
        };
    }
}

public class ResolvedIssue : Issue
{
    public required DateTimeOffset ResolvedOn { get; set; }
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

