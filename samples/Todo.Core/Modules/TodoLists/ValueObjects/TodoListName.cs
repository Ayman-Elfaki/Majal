namespace Todo.Core.Modules.TodoLists.ValueObjects;

/// <summary>
/// The todoList name value object
/// </summary>
[ValueObject<string>]
public readonly partial struct TodoListName
{
    public const int MaxLength = 200;
}