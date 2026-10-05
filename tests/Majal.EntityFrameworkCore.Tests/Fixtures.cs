namespace Majal.EntityFrameworkCore.Tests;

[Entity, Archivable]
public partial class Note
{
    // Public constructor for testing
    public Note()
    {
    }

    public string Text { get; set; } = string.Empty;
}

[Entity, Auditable]
public partial class LogEntry
{
    // Public constructor for testing
    public LogEntry()
    {
    }

    public string Message { get; set; } = string.Empty;
}

[Entity, Translatable<string>]
public partial class Article
{
    // Public constructor for testing
    public Article()
    {
    }

    public string Title { get; set; } = string.Empty;
}

[ValueObject<string>]
public readonly partial struct Email
{
    internal const int MaxLength = 100;
}

public enum CustomerTier
{
    Standard,
    Premium,
    Vip
}

[ValueObject<CustomerTier>]
public readonly partial struct Tier;

[Entity]
public partial class Customer
{
    // Public constructor for testing
    public Customer()
    {
    }

    public required Email Email { get; set; }
    public Tier Tier { get; set; } = Tier.Standard;
}

[Entity, Ordinal]
public partial class Todo
{
    // Public constructor for testing
    public Todo()
    {
    }

    public string Title { get; set; } = string.Empty;
}

[Entity<string>, Ordinal]
public partial class TaggedTodo
{
    // Public constructor for testing
    public TaggedTodo()
    {
    }

    public string Title { get; set; } = string.Empty;
}