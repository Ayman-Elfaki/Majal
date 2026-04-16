using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Projects.Entities;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Endpoints;

public static class CreateProjectEndpoint
{
    public class CreateProjectRequest
    {
        public required ProjectName Name { get; set; }
        public required IEnumerable<ProjectTranslationDto> Translations { get; set; } = [];

        public class ProjectTranslationDto
        {
            public required ProjectName Name { get; init; }
            public required ProjectDescription Description { get; init; }
            public required string Locale { get; init; }
        }
    }

    public static void MapCreateProjectEndpoint(this WebApplication app)
    {
        app.MapPost("/projects", async (CreateProjectRequest req, AppDbContext context, CancellationToken ct) =>
        {
            var translations = req.Translations
                .Select(t => ProjectTranslation.Create(t.Name, t.Description, t.Locale))
                .ToArray();

            var project = Project.Create(req.Name, translations);

            context.Projects.Add(project);

            await context.SaveChangesAsync(ct);

            return Results.Ok();
        });
    }
}