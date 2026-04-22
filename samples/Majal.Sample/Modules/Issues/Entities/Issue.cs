using Majal.Sample.Modules.Issues.ValueObjects;
using Majal.Sample.Modules.Projects.Entities;

namespace Majal.Sample.Modules.Issues.Entities;

[Entity, Aggregate]
[Ordinal, Archivable, Auditable]
public partial class Issue
{
    public required IssueTitle Title { get; set; }
    public required IssuePriority Priority { get; set; }
    public required IssueStoryPoints StoryPoints { get; set; }
    public Project Project { get; set; } = null!;

    public static Issue Create(IssueTitle title, IssuePriority priority, IssueStoryPoints storyPoints)
    {
        return new Issue
        {
            Ordinal = 0,
            Title = title,
            Priority = priority,
            StoryPoints = storyPoints,
        };
    }
}