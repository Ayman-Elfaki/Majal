using System.Globalization;
using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Majal.Sample.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ILocaleProvider<CultureInfo> localeProvider)
    : DbContext(options), ITranslatableDbContext<CultureInfo>
{
    public CultureInfo Locale => localeProvider.GetCurrentLocale();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Project> Projects => Set<Project>();
    
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        builder.RegisterValueObjectsConventions();
        builder.Properties<CultureInfo>().HaveConversion<CultureInfoValueConverter>();
        builder.Conventions.Add(_ => new ArchivableFilterConvention());
        builder.Conventions.Add(_ => new TranslatableFilterConvention<CultureInfo, AppDbContext>(this));
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

    public sealed class CultureInfoValueConverter() :
        ValueConverter<CultureInfo, string>(p => p.IetfLanguageTag, p => CultureInfo.GetCultureInfoByIetfLanguageTag(p))
    {
    }
}