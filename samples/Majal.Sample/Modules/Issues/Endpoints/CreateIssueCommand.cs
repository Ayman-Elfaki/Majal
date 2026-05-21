using FluentValidation;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Issues.ValueObjects;
using Majal.Sample.Modules.Projects.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace Majal.Sample.Modules.Issues.Endpoints;

/// <summary>
/// Create a new issue
/// </summary>
public partial record CreateIssueCommand
{
    [DtoFor<PendingIssue>(Prefix = "")]
    [FlattenDtoFor<Capacity>(IsReversed = true)]
    public partial record WqIssueDto;

    /// <summary>
    /// request validator
    /// </summary>
    public class Validator : AbstractValidator<WqIssueDto>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Validator()
        {
            RuleFor(dto => dto.Title).NotEmpty().MaximumLength(IssueTitle.MaxLength);
            RuleFor(dto => dto.StoryPoints).InclusiveBetween(0, 10);
            RuleFor(dto => dto.Priority).InclusiveBetween(0, 5);
        }
    }

    /// <summary>
    /// Assign an issue to a project
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Projects")]
    [WolverinePost("/projects/{id:int}/issues")]
    public static async Task<IResult> Create(int id, WqIssueDto dto, [FromServices] AppDbContext context,
        CancellationToken ct)
    {
        var project = context.Projects.FirstOrDefault(p => p.Id == id);

        if (project is null) return Results.NotFound();

        var issue = PendingIssue.Create(
            IssueTitle.Create(dto.Title),
            IssuePriority.Create(dto.StoryPoints),
            IssueStoryPoints.Create(dto.StoryPoints),
            project
        );

        project.Issues.Add(issue);
        await context.SaveChangesAsync(ct);
        return Results.Ok();
    }
}