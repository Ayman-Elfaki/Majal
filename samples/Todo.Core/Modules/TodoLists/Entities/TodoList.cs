using Todo.Core.Modules.TodoItems.Entities;
using Todo.Core.Modules.TodoLists.ValueObjects;

namespace Todo.Core.Modules.TodoLists.Entities;

/// <summary>
/// TodoList entity
/// </summary>
[Entity, Aggregate]
[Archivable, Auditable, Ordinal]
public abstract partial class TodoList
{
    /// <summary>
    /// The name of the todoList
    /// </summary>
    public required TodoListName Name { get; init; }

    /// <summary>
    /// The todos of the todoList
    /// </summary>
    public ICollection<TodoItem> TodoItems { get; set; } = [];

    /// <summary>
    /// The translations of the todoList
    /// </summary>
    public ICollection<TodoListTranslation> Translations { get; protected set; } = [];
}