using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Issues.Entities;
using Microsoft.EntityFrameworkCore;

namespace Majal.Sample.Modules.Issues.Endpoints;

public static class ResolveIssueEndpoint
{
    public static void MapResolveIssueEndpoint(this WebApplication app)
    {
        app.MapPost("/issues/{id:int}/resolve", async (int id, AppDbContext context, CancellationToken ct) =>
        {
            var issue = context.Issues
                .Include(p => p.Project)
                .OfType<PendingIssue>()
                .FirstOrDefault(p => p.Id == id);

            if (issue is null) return Results.NotFound();

            var resolvedIssue = ResolvedIssue.Create(issue, DateTimeOffset.UtcNow);
            context.Issues.Add(resolvedIssue);
            issue.Project.Issues.Remove(issue);

            await context.SaveChangesAsync(ct);
            return Results.Ok();
        });
    }
}