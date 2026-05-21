using Majal.Sample.Common.Persistence;
using Majal.Sample.Modules.Issues.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Majal.Sample.Modules.Issues.Endpoints;

/// <summary>
/// resolve an existing issue
/// </summary>
public class ResolveIssueCommand
{
    /// <summary>
    /// Resolve an issue
    /// </summary>
    /// <param name="id"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Issues")]
    [WolverinePost("/issues/{id:int}/resolve")]
    public static async Task<IResult> Resolve(int id, [FromServices] AppDbContext context, CancellationToken ct)
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
    }
}