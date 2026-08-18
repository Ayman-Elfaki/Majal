using Microsoft.EntityFrameworkCore;

namespace Majal.EntityFrameworkCore.Tests;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options, string locale = "en-US")
    : MajalDbContext<string>(options, locale)
{
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Customer> Customers => Set<Customer>();
}