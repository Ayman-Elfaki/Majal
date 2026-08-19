using Todo.Core.Common.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace Todo.Api.Modules.TodoLists.Endpoints;

/// <summary>
/// List all todoLists
/// </summary>
public class ListTodoListsQuery
{
    /// <summary>
    /// The Query Response
    /// </summary>
    public class ListTodoListsResponse
    {
        /// <summary>
        /// The todoLists
        /// </summary>
        public IEnumerable<TodoListDto> TodoLists { get; set; } = [];
    }

    /// <summary>
    /// the dto for an todo
    /// </summary>
    public class TodoItemDto
    {
        /// <summary>
        /// The todo title
        /// </summary>
        public required string Title { get; set; }
        /// <summary>
        /// The todo priority
        /// </summary>
        public required int Priority { get; set; }
        /// <summary>
        /// The todo story points
        /// </summary>
        public required int StoryPoints { get; set; }
    }

    /// <summary>
    /// the dto for a todoList
    /// </summary>
    public class TodoListDto
    {
        /// <summary>
        /// The locale
        /// </summary>
        public required string Locale { get; init; }
        /// <summary>
        /// The todoList name
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The todoList description
        /// </summary>
        public required string DisplayName { get; init; }
        /// <summary>
        /// The todoList description
        /// </summary>
        public required string Description { get; init; }
        /// <summary>
        /// The todos
        /// </summary>
        public IEnumerable<TodoItemDto> TodoItems { get; set; } = [];
    }
    
    /// <summary>
    /// List all todoLists
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("TodoLists")]
    [WolverineGet("/todo-lists")]
    [ProducesResponseType<ListTodoListsResponse>(200)]
    public static async Task<IResult> List([FromServices] AppDbContext context, CancellationToken ct)
    {
        var todoListsQuery = await context.TodoLists
            .AsNoTracking()
            .AsSplitQuery()
            .Select(p => new { p.Name, p.Translations, p.TodoItems })
            .ToListAsync(ct);

        var todoLists =
            from todoList in todoListsQuery
            let translation = todoList.Translations.First()
            select new TodoListDto
            {
                Name = todoList.Name,
                DisplayName = translation.DisplayName,
                Description = translation.Description,
                Locale = translation.Locale.ToString(),
                TodoItems = todoList.TodoItems.Select(i => new TodoItemDto
                    { Title = i.Title, Priority = i.Priority, StoryPoints = i.StoryPoints })
            };

        return Results.Ok(new ListTodoListsResponse { TodoLists = todoLists });
    }
}