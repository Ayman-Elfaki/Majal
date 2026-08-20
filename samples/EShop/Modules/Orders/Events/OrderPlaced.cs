using EShop.Modules.Catalog.ValueObjects;

namespace EShop.Modules.Orders.Events;

/// <summary>Raised once an order has been placed and persisted.</summary>
public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, Money Total) : OrderEvent;