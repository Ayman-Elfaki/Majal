namespace Todo.Core.Modules.TodoItems.ValueObjects;

/// <summary>
/// The todo title value object
/// </summary>
[ValueObject<string>]
public readonly partial struct TodoTitle
{
    public const int MaxLength = 200;
}