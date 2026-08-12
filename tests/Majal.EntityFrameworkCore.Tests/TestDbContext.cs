using Microsoft.EntityFrameworkCore;

namespace Majal.EntityFrameworkCore.Tests;

public sealed class TestDbContext(DbContextOptions<TestDbContext> options, string locale = "en-US")
    : DbContext(options), ITranslatableDbContext<string>
{
    public string Locale { get; } = locale;

    public DbSet<Note> Notes => Set<Note>();
    public DbSet<LogEntry> LogEntries => Set<LogEntry>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        builder.RegisterValueObjectsConventions();
        builder.Conventions.Add(_ => new ArchivableFilterConvention());
        builder.Conventions.Add(_ => new TranslatableFilterConvention<string, TestDbContext>(this));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .AddInterceptors(new AuditableSaveChangesInterceptor())
            .AddInterceptors(new ArchivableSaveChangesInterceptor());
    }
}