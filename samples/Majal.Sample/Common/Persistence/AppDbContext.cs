using System.Globalization;
using Majal.Sample.Modules.Issues.Entities;
using Majal.Sample.Modules.Projects.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Majal.Sample.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ILocaleProvider<CultureInfo> localeProvider)
    : MajalDbContext<CultureInfo>(options, localeProvider.GetCurrentLocale())
{
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        base.ConfigureConventions(builder);
        builder.Properties<CultureInfo>().HaveConversion<CultureInfoValueConverter>();
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