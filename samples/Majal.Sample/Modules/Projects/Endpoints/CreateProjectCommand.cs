using FluentValidation;
using Majal.Sample.Common.Filters;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Projects.Entities;
using Majal.Sample.Modules.Projects.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Majal.Sample.Modules.Projects.Endpoints;

/// <summary>
/// create a new project
/// </summary>
public partial class CreateProjectCommand
{
    /// <summary>
    /// The Project Dto
    /// </summary>
    [DtoFor<StrategicProject>]
    [FlattenDtoFor<Capacity>(IsReversed = true)]
    public partial class StrategicProjectDtos;

    /// <summary>
    /// The Dto Validator
    /// </summary>
    public class Validator : AbstractValidator<StrategicProjectDtos>
    {
        /// <summary>
        /// the validator constructor
        /// </summary>
        public Validator()
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

    /// <summary>
    /// Create New Project
    /// </summary>
    /// <param name="req"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Projects")]
    [WolverinePost("/projects")]
    public static async Task<IResult> Create(StrategicProjectDtos req, [FromServices] AppDbContext context, CancellationToken ct)
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
    }
}