using Todo.Core.Common.Persistence;
using Todo.Core.Modules.TodoItems.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Todo.Api.Modules.TodoItems.Endpoints;

/// <summary>
/// resolve an existing todo
/// </summary>
public class ResolveTodoItemCommand
{
    /// <summary>
    /// Resolve an todo
    /// </summary>
    /// <param name="id"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("TodoItems")]
    [WolverinePost("/todos/{id:int}/resolve")]
    public static async Task<IResult> Resolve(int id, [FromServices] AppDbContext context, CancellationToken ct)
    {
        var todo = context.TodoItems
            .Include(p => p.TodoList)
            .OfType<PendingTodoItem>()
            .FirstOrDefault(p => p.Id == id);

        if (todo is null) return Results.NotFound();

        var resolvedTodoItem = CompletedTodoItem.Create(todo, DateTimeOffset.UtcNow);
        context.TodoItems.Add(resolvedTodoItem);
        todo.TodoList.TodoItems.Remove(todo);

        await context.SaveChangesAsync(ct);
        return Results.Ok();
    }
}