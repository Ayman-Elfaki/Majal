using System.ComponentModel.DataAnnotations;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Projects.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Majal.Sample.Modules.Projects.Endpoints;

public static class ListProjectsEndpoint
{
    public class ListProjectsResponse
    {
        public IEnumerable<ProjectDto> Projects { get; set; } = [];
    }

    public class ProjectDto
    {
        public required ProjectName Name { get; set; }
        public required ProjectName DisplayName { get; init; }
        public required ProjectDescription Description { get; init; }
        public required string Locale { get; init; }
    }

    public static void MapListProjectsEndpoint(this WebApplication app)
    {
        app.MapGet("/projects", async (AppDbContext context, CancellationToken ct) =>
        {
            var projectsQuery = await context.Projects
                .AsNoTracking()
                .Select(p => new { p.Name, p.Translations })
                .ToListAsync(ct);

            var projects =
                from project in projectsQuery
                let translation = project.Translations.First()
                select new ProjectDto
                {
                    Name = project.Name,
                    DisplayName = translation.DisplayName,
                    Description = translation.Description,
                    Locale = translation.Locale
                };

            return Results.Ok(new ListProjectsResponse { Projects = projects });
        });
    }
}