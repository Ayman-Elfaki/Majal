using Majal.Sample.Common.Persistence;
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
    /// The Query Response
    /// </summary>
    public class ListProjectsResponse
    {
        /// <summary>
        /// The projects
        /// </summary>
        public IEnumerable<ProjectDto> Projects { get; set; } = [];
    }

    /// <summary>
    /// the dto for an issue
    /// </summary>
    public class IssueDto
    {
        /// <summary>
        /// The issue title
        /// </summary>
        public required string Title { get; set; }
        /// <summary>
        /// The issue priority
        /// </summary>
        public required int Priority { get; set; }
        /// <summary>
        /// The issue story points
        /// </summary>
        public required int StoryPoints { get; set; }
    }

    /// <summary>
    /// the dto for a project
    /// </summary>
    public class ProjectDto
    {
        /// <summary>
        /// The locale
        /// </summary>
        public required string Locale { get; init; }
        /// <summary>
        /// The project name
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The project description
        /// </summary>
        public required string DisplayName { get; init; }
        /// <summary>
        /// The project description
        /// </summary>
        public required string Description { get; init; }
        /// <summary>
        /// The issues
        /// </summary>
        public IEnumerable<IssueDto> Issues { get; set; } = [];
    }
    
    /// <summary>
    /// List all projects
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Projects")]
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