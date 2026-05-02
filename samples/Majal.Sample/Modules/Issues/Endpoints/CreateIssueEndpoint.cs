using System.ComponentModel.DataAnnotations;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Issues.ValueObjects;

namespace Majal.Sample.Modules.Issues.Endpoints;

public class CreateIssueRequest
{
    [Required]
    [MaxLength(IssueTitle.MaxLength)]
    public required string Title { get; init; }

    [Required] [Range(0, 10)] public required int StoryPoint { get; init; }

    [Required] [Range(0, 5)] public required int Priority { get; init; }
}

public static class CreateIssueEndpoint
{
    public static void MapCreateIssueEndpoint(this WebApplication app)
    {
        app.MapPost("/projects/{id:int}/issues",
            async (int id, CreateIssueRequest req, AppDbContext context, CancellationToken ct) =>
            {
                var project = context.Projects.FirstOrDefault(p => p.Id == id);

                if (project is null) return Results.NotFound();

                var issue = PendingIssue.Create(
                    IssueTitle.Create(req.Title),
                    IssuePriority.Create(req.StoryPoint),
                    IssueStoryPoints.Create(req.StoryPoint)
                );

                project.Issues.Add(issue);
                await context.SaveChangesAsync(ct);
                return Results.Ok();
            });
    }
}