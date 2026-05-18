using Majal.Sample.Modules.Issues.ValueObjects;
using Majal.Sample.Modules.Projects.Entities;

namespace Majal.Sample.Modules.Issues.Entities;

/// <summary>
/// The pending issue entity
/// </summary>
public class PendingIssue : Issue
{
    /// <summary>
    /// Create a pending issue
    /// </summary>
    /// <param name="title">the title of the issue</param>
    /// <param name="priority">the priority of the issue</param>
    /// <param name="storyPoints">the story points of the issue</param>
    /// <param name="project">the project of the issue</param>
    /// <returns>The created pending issue</returns>
    public static PendingIssue Create(IssueTitle title, IssuePriority priority, IssueStoryPoints storyPoints, Project project)
    {
        return new PendingIssue
        {
            Ordinal = 0,
            Title = title,
            Priority = priority,
            StoryPoints = storyPoints,
            Project = project
        };
    }
}