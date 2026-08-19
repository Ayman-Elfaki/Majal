using Todo.Core.Common.Extensions;
using Todo.Core.Modules.TodoLists.ValueObjects;

namespace Todo.Core.Modules.TodoLists.Entities;

/// <summary>
/// Strategic TodoList
/// </summary>
public class SharedTodoList : TodoList
{
    /// <summary>
    /// Create a todoList
    /// </summary>
    /// <param name="name">The name of the todoList</param>
    /// <param name="translations">The translations for the todoList</param>
    /// <returns>The created todoList</returns>
    /// <exception cref="ArgumentException">The translation must include all required locales.</exception>
    public static SharedTodoList Create(TodoListName name, TodoListTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");

        return new SharedTodoList
        {
            Ordinal = 1,
            Name = name,
            Translations = [.. translations]
        };
    }
}