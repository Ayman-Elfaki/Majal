using System.ComponentModel.DataAnnotations;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Common.Validators;
using Majal.Sample.Modules.Projects.Entities;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Endpoints;

public static class CreateProjectEndpoint
{
    public class Request
    {
        [Required]
        [MaxLength(ProjectName.MaxLength)]
        public required string Name { get; init; }

        [TranslatablesValidator] public IEnumerable<ProjectTranslationDto> Translations { get; init; } = [];

        public class ProjectTranslationDto : ITranslatable<string>
        {
            [Required]
            [MaxLength(ProjectName.MaxLength)]
            public required string DisplayName { get; init; }

            [Required]
            [MaxLength(ProjectDescription.MaxLength)]
            public required string Description { get; init; }

            [Required]
            [RegularExpression("^[a-zA-Z]{2}$")]
            public required string Locale { get; init; }
        }
    }

    public static void MapCreateProjectEndpoint(this WebApplication app)
    {
        app.MapPost("/projects", async (Request req, AppDbContext context, CancellationToken ct) =>
        {
            var translations = req.Translations
                .Select(t =>
                    ProjectTranslation.Create(
                        ProjectName.From(t.DisplayName),
                        ProjectDescription.From(t.Description),
                        t.Locale
                    )
                ).ToArray();

            var project = Project.Create(ProjectName.From(req.Name), translations);

            context.Projects.Add(project);

            await context.SaveChangesAsync(ct);

            return Results.Ok();
        });
    }
}