using EShop.Modules.Customers.ValueObjects;
using EShop.Modules.Orders.Entities;

namespace EShop.Modules.Customers.Entities;

/// <summary>Marker base for domain events raised by <see cref="Customer"/>.</summary>
public abstract record CustomerEvent;

/// <summary>
/// Uses the explicit <c>[Aggregate&lt;CustomerEvent&gt;]</c> form and pre-declares its own <c>Id</c>
/// property, so <c>[Entity&lt;Guid&gt;]</c> skips generating one (contrast with <see cref="Order"/>,
/// which lets the generator add its <c>Id</c>).
/// </summary>
[Entity<Guid>, Aggregate<CustomerEvent>]
public partial class Customer
{
    public Guid Id { get; private init; }
    public CustomerName Name { get; private init; } = default!;
    public CustomerEmail Email { get; private init; } = default!;

    public static Customer Create(CustomerName name, CustomerEmail email) =>
        new() { Id = Guid.NewGuid(), Name = name, Email = email };
}
