using Todo.Core.Modules.TodoItems.ValueObjects;
using Todo.Core.Modules.TodoLists.Entities;

namespace Todo.Core.Modules.TodoItems.Entities;

/// <summary>
/// The todo entity
/// </summary>
[Entity<int>, Aggregate]
[Ordinal, Archivable, Auditable]
public abstract partial class TodoItem
{
    /// <summary>
    /// the title of the todo
    /// </summary>
    public required TodoTitle Title { get; set; }

    /// <summary>
    /// the priority of the todo
    /// </summary>
    public required TodoPriority Priority { get; set; }

    /// <summary>
    /// the story points of the todo
    /// </summary>
    public required TodoStoryPoints StoryPoints { get; set; }

    /// <summary>
    /// the todoList of the todo
    /// </summary>
    public TodoList TodoList { get; set; } = null!;
}