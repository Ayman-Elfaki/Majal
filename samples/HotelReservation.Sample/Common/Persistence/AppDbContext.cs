using System.Globalization;
using HotelReservation.Sample.Modules.Hotels.Entities;
using HotelReservation.Sample.Modules.Reservations.Entities;
using HotelReservation.Sample.Modules.Rooms.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace HotelReservation.Sample.Common.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ILocaleProvider<CultureInfo> localeProvider)
    : DbContext(options), ITranslatableDbContext<CultureInfo>
{
    public CultureInfo Locale => localeProvider.GetCurrentLocale();
    public DbSet<Hotel> Hotels => Set<Hotel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

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
