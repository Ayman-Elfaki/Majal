using EShop.Modules.Products.ValueObjects;

namespace EShop.Modules.Orders.Events;

/// <summary>Raised once an order has been placed and persisted.</summary>
public sealed record OrderPlaced(Guid OrderId, Guid CustomerId, Money Total) : OrderEvent;