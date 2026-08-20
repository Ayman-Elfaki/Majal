using EShop.Modules.Catalog.Entities;
using EShop.Modules.Catalog.ValueObjects;
using EShop.Modules.Customers.Entities;
using EShop.Modules.Orders.Events;

namespace EShop.Modules.Orders.Entities;


/// <summary>
/// Order aggregate root. Lets the generator add its own <c>Id</c> property (contrast with
/// <see cref="Customer"/>, which pre-declares its own) and actually publishes and clears a domain event,
/// unlike <see cref="Product"/>'s bare, unused <c>[Aggregate]</c> declaration.
/// </summary>
[Entity<Guid>, Aggregate<OrderEvent>]
[Auditable]
public partial class Order
{
    public Guid CustomerId { get; private init; }

    /// <summary>
    /// Named differently from the "lines" factory parameter for the same reason as <see cref="Payment"/>:
    /// <see cref="OrderLine"/>'s product ID is supplied by the order line entity (it
    /// references <see cref="Product"/> by aggregate reference, which isn't
    /// a readable property here), so a matching name would make the generator try a nested-collection
    /// forwarding call that can't supply it.
    /// </summary>
    public List<OrderLine> LineItems { get; private init; } = [];

    /// <summary>
    /// Named differently from the "paymentMethod" factory parameter on purpose: <see cref="PaymentMethod"/>
    /// is an abstract polymorphic type, so a property matching the
    /// parameter name would make the DTO generator try (and fail) to auto-convert it. This naming makes
    /// the parameter fall back to the generator's explicit "supplied argument" path instead.
    /// </summary>
    public PaymentMethod Payment { get; private init; } = null!;

    public static Order Create(Customer customer, IReadOnlyList<OrderLine> lines, PaymentMethod paymentMethod)
    {
        var total = Money.Create(lines.Sum(l => l.UnitPrice.Amount * l.Quantity), lines[0].UnitPrice.Currency);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            LineItems = [.. lines],
            Payment = paymentMethod
        };

        order.Publish(new OrderPlaced(order.Id, customer.Id, total));
        return order;
    }
}
