using FluentValidation;
using Majal.Sample.Common.Filters;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Projects.Entities;
using Majal.Sample.Modules.Projects.ValueObjects;

namespace Majal.Sample.Modules.Projects.Endpoints;

[DtoFor<OperationalProject>]
public partial record ProjectDto;

internal class ProjectDtoValidator : AbstractValidator<ProjectDto>
{
    public ProjectDtoValidator()
    {
        RuleFor(dto => dto.Name)
            .NotEmpty()
            .MaximumLength(ProjectName.MaxLength);

        RuleFor(dto => dto.Translations)
            .NotEmpty();

        RuleForEach(dto => dto.Translations).ChildRules(r =>
        {
            r.RuleFor(p => p.DisplayName)
                .NotEmpty()
                .MaximumLength(ProjectName.MaxLength);

            r.RuleFor(p => p.Description)
                .NotEmpty()
                .MaximumLength(ProjectDescription.MaxLength);

            r.RuleFor(p => p.Locale)
                .NotEmpty()
                .Matches("^[a-zA-Z]{2}$");
        });
    }
}

internal static class CreateProjectEndpoint
{
    public static void MapCreateProjectEndpoint(this WebApplication app)
    {
        app.MapPost("/projects", async (ProjectDto req, AppDbContext context, CancellationToken ct) =>
        {
            var translations = req.Translations
                .Select(t =>
                    ProjectTranslation.Create(
                        ProjectName.Create(t.DisplayName),
                        ProjectDescription.Create(t.Description),
                        t.Locale
                    )
                ).ToArray();

            var project = OperationalProject.Create(
                ProjectName.Create(req.Name),
                translations
            );

            context.Projects.Add(project);
            await context.SaveChangesAsync(ct);

            return Results.Ok();
        }).AddEndpointFilter<ValidationFilter<ProjectDtoValidator>>();
    }
}