using EShop.Modules.Catalog.Entities;
using EShop.Modules.Orders.Events;

namespace EShop.Modules.Orders.Handlers;

/// <summary>
/// Picked up by Wolverine's convention-based handler discovery and invoked when an <see cref="OrderPlaced"/>
/// event is published -- the one domain event in this sample that's actually dispatched end-to-end, unlike
/// <see cref="Product"/>'s bare, unused <c>[Aggregate]</c> declaration.
/// </summary>
public class OrderPlacedHandler
{
    public void Handle(OrderPlaced @event, ILogger<OrderPlacedHandler> logger)
    {
        logger.LogInformation(
            "Order {OrderId} placed by customer {CustomerId} for {Total} {Currency}",
            @event.OrderId, @event.CustomerId, @event.Total.Amount, @event.Total.Currency);
    }
}
