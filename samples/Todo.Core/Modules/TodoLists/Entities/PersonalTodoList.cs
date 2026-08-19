using Todo.Core.Common.Extensions;
using Todo.Core.Modules.TodoLists.ValueObjects;

namespace Todo.Core.Modules.TodoLists.Entities;

/// <summary>
/// Strategic TodoList
/// </summary>
public class PersonalTodoList : TodoList
{
    /// <summary>
    /// Create a todoList
    /// </summary>
    /// <param name="name">The name of the todoList</param>
    /// <param name="translations">The translations for the todoList</param>
    /// <param name="isImportant">The importance of the todoList</param>
    /// <param name="capacity">The todoList capacity</param>
    /// <returns>The created todoList</returns>
    /// <exception cref="ArgumentException">The translation must include all required locales.</exception>
    public static PersonalTodoList Create(TodoListName name,Capacity capacity, bool isImportant, TodoListTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");

        return new PersonalTodoList
        {
            Ordinal = 1,
            Name = name,
            Translations = [.. translations]
        };
    }
}