using System.Globalization;
using EShop.Modules.Catalog.Entities;
using EShop.Modules.Customers.Entities;
using EShop.Modules.Orders.Entities;
using EShop.Modules.Orders.Events;
using EShop.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace EShop.Persistence;

public sealed class EShopDbContext(DbContextOptions<EShopDbContext> options,
    ILocaleProvider<CultureInfo> localeProvider)
    : MajalDbContext<CultureInfo>(options, localeProvider.GetCurrentLocale())
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<CultureInfo>().HaveConversion<CultureInfoValueConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // [Aggregate<TDomainEvent>]'s generated `Events` property is an IEnumerable<TDomainEvent>, which EF
        // Core's default conventions would otherwise try to map as a collection navigation.
        modelBuilder.Ignore<CustomerEvent>();
        modelBuilder.Ignore<OrderEvent>();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EShopDbContext).Assembly);
    }
}
