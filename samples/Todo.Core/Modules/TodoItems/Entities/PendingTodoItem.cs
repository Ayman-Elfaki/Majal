using Todo.Core.Modules.TodoItems.ValueObjects;
using Todo.Core.Modules.TodoLists.Entities;

namespace Todo.Core.Modules.TodoItems.Entities;

/// <summary>
/// The pending todo entity
/// </summary>
public class PendingTodoItem : TodoItem
{
    /// <summary>
    /// Create a pending todo
    /// </summary>
    /// <param name="title">the title of the todo</param>
    /// <param name="priority">the priority of the todo</param>
    /// <param name="storyPoints">the story points of the todo</param>
    /// <param name="todoList">the todoList of the todo</param>
    /// <returns>The created pending todo</returns>
    public static PendingTodoItem Create(TodoTitle title, TodoPriority priority, TodoStoryPoints storyPoints, TodoList todoList)
    {
        return new PendingTodoItem
        {
            Ordinal = 0,
            Title = title,
            Priority = priority,
            StoryPoints = storyPoints,
            TodoList = todoList
        };
    }
}