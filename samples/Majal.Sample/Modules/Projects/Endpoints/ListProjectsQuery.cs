using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Issues.ValueObjects;
using Majal.Sample.Modules.Projects.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Majal.Sample.Modules.Projects.Endpoints;

/// <summary>
/// List all projects
/// </summary>
public class ListProjectsQuery
{
    /// <summary>
    /// the response
    /// </summary>
    public class ListProjectsResponse
    {
        public IEnumerable<ProjectDto> Projects { get; set; } = [];
    }

    /// <summary>
    /// the dto for an issue
    /// </summary>
    public class IssueDto
    {
        public required string Title { get; set; }
        public required int Priority { get; set; }
        public required int StoryPoints { get; set; }
    }

    /// <summary>
    /// the dto for a project
    /// </summary>
    public class ProjectDto
    {
        public required string Locale { get; init; }
        public required string Name { get; set; }
        public required string DisplayName { get; init; }
        public required string Description { get; init; }
        public IEnumerable<IssueDto> Issues { get; set; } = [];
    }

    [WolverineGet("/projects")]
    [ProducesResponseType<ListProjectsResponse>(200)]
    public static async Task<IResult> List([FromServices] AppDbContext context, CancellationToken ct)
    {
        var projectsQuery = await context.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Select(p => new { p.Name, p.Translations, p.Issues })
            .ToListAsync(ct);

        var projects =
            from project in projectsQuery
            let translation = project.Translations.First()
            select new ProjectDto
            {
                Name = project.Name,
                DisplayName = translation.DisplayName,
                Description = translation.Description,
                Locale = translation.Locale.ToString(),
                Issues = project.Issues.Select(i => new IssueDto
                    { Title = i.Title, Priority = i.Priority, StoryPoints = i.StoryPoints })
            };

        return Results.Ok(new ListProjectsResponse { Projects = projects });
    }
}