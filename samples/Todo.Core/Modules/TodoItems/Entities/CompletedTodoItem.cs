namespace Todo.Core.Modules.TodoItems.Entities;

/// <summary>
/// Resolved TodoItem entity
/// </summary>
public class CompletedTodoItem : TodoItem
{
    /// <summary>
    /// The date and time the todo was resolved
    /// </summary>
    public required DateTimeOffset ResolvedOn { get; set; }


    /// <summary>
    /// Create a resolved todo
    /// </summary>
    /// <param name="todo">The todo to resolve</param>
    /// <param name="resolvedOn">The date and time the todo was resolved</param>
    /// <returns>The created resolved todo</returns>
    public static CompletedTodoItem Create(PendingTodoItem todo, DateTimeOffset resolvedOn)
    {
        return new CompletedTodoItem
        {
            Ordinal = 0,
            Title = todo.Title,
            Priority = todo.Priority,
            StoryPoints = todo.StoryPoints,
            ResolvedOn = resolvedOn,
            TodoList = todo.TodoList
        };
    }
}