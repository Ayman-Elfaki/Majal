using Majal.Sample.Modules.Projects.Entities;
using Microsoft.EntityFrameworkCore;

namespace Majal.Sample.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ILocaleProvider localeProvider)
    : DbContext(options), ITranslatableDbContext
{
    public string CurrentLocale => localeProvider.GetCurrentLocale();

    public DbSet<Project> Projects => Set<Project>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        builder.RegisterValueObjectsConventions();
        builder.Conventions.Add(_ => new ArchivableFilterConvention());
        builder.Conventions.Add(_ => new TranslatableFilterConvention<AppDbContext>(this));
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .AddInterceptors(new AuditableSaveChangesInterceptor())
            .AddInterceptors(new ArchivableSaveChangesInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}