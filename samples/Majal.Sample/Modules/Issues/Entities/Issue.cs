using Majal.Sample.Modules.Issues.ValueObjects;
using Majal.Sample.Modules.Projects.Entities;

namespace Majal.Sample.Modules.Issues.Entities;

/// <summary>
/// The issue entity
/// </summary>
[Entity, Aggregate]
[Ordinal, Archivable, Auditable]
public abstract partial class Issue
{
    /// <summary>
    /// the title of the issue
    /// </summary>
    public required IssueTitle Title { get; set; }

    /// <summary>
    /// the priority of the issue
    /// </summary>
    public required IssuePriority Priority { get; set; }

    /// <summary>
    /// the story points of the issue
    /// </summary>
    public required IssueStoryPoints StoryPoints { get; set; }

    /// <summary>
    /// the project of the issue
    /// </summary>
    public Project Project { get; set; } = null!;
}