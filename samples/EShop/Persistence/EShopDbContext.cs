using EShop.Modules.Categories.Entities;
using EShop.Modules.Customers.Entities;
using EShop.Modules.Orders.Entities;
using EShop.Modules.Orders.Events;
using EShop.Modules.Products.Entities;
using Microsoft.EntityFrameworkCore;

namespace EShop.Persistence;

public sealed class EShopDbContext(
    DbContextOptions<EShopDbContext> options,
    ILocaleProvider<string> localeProvider)
    : MajalDbContext<string>(options, localeProvider.GetCurrentLocale())
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<Customer> Customers => Set<Customer>();

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