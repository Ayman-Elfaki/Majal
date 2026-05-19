using FluentValidation;
using Majal.Sample.Common.Filters;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Issues.ValueObjects;

namespace Majal.Sample.Modules.Issues.Endpoints;

[DtoFor<PendingIssue>]
internal partial class IssueDto;

internal class IssueDtoValidator : AbstractValidator<IssueDto>
{
    public IssueDtoValidator()
    {
        RuleFor(dto => dto.Title).NotEmpty().MaximumLength(IssueTitle.MaxLength);
        RuleFor(dto => dto.StoryPoints).InclusiveBetween(0, 10);
        RuleFor(dto => dto.Priority).InclusiveBetween(0, 5);
    }
}

internal static class CreateIssueEndpoint
{
    public static void MapCreateIssueEndpoint(this WebApplication app)
    {
        app.MapPost("/projects/{id:int}/issues",
            async (int id, IssueDto dto, AppDbContext context, CancellationToken ct) =>
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
            }).AddEndpointFilter<ValidationFilter<IssueDtoValidator>>();
    }
}