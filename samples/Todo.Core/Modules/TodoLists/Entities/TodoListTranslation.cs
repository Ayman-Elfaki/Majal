using System.Globalization;
using Todo.Core.Common.Extensions;
using Todo.Core.Modules.TodoLists.ValueObjects;

namespace Todo.Core.Modules.TodoLists.Entities;

/// <summary>
/// The todoList translation entity
/// </summary>
[Entity]
[Translatable<CultureInfo>]
public partial class TodoListTranslation
{
    /// <summary>
    /// The display name for the todoList
    /// </summary>
    public required TodoListName DisplayName { get; set; }

    /// <summary>
    /// The description for the todoList
    /// </summary>
    public required TodoListDescription Description { get; set; }

    /// <summary>
    /// Create a todoList translation
    /// </summary>
    /// <param name="displayName">The display name for the todoList</param>
    /// <param name="description">The description for the todoList</param>
    /// <param name="locale">The locale for the todoList translation</param>
    /// <returns>The created todoList translation</returns>
    /// <exception cref="NotSupportedException">Thrown if the locale is not supported</exception>
    public static TodoListTranslation Create(TodoListName displayName, TodoListDescription description, string locale)
    {
        if (!locale.IsLocaleSupported())
            throw new NotSupportedException($"Language {locale} is not supported");

        return new TodoListTranslation
        {
            DisplayName = displayName,
            Description = description,
            Locale = CultureInfo.GetCultureInfoByIetfLanguageTag(locale)
        };
    }
}