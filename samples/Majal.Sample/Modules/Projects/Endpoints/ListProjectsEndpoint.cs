using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Issues.ValueObjects;
using Majal.Sample.Modules.Projects.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Majal.Sample.Modules.Projects.Endpoints;

public static class ListProjectsEndpoint
{
    public class ListProjectsResponse
    {
        public IEnumerable<ProjectDto> Projects { get; set; } = [];
    }

    public class IssueDto
    {
        public required IssueTitle Title { get; set; }
    }
    
    public class ProjectDto
    {
        public required ProjectName Name { get; set; }
        public required ProjectName DisplayName { get; init; }
        public required ProjectDescription Description { get; init; }
        public required string Locale { get; init; }
        public IEnumerable<IssueDto> Issues { get; set; } = [];
    }

    public static void MapListProjectsEndpoint(this WebApplication app)
    {
        app.MapGet("/projects", async (AppDbContext context, CancellationToken ct) =>
        {
            var projectsQuery = await context.Projects
                .AsNoTracking()
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
                    Issues = project.Issues.Select(i => new IssueDto { Title = i.Title })
                };

            return Results.Ok(new ListProjectsResponse { Projects = projects });
        });
    }
}