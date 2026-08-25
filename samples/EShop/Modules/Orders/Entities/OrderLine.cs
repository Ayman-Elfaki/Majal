using EShop.Modules.Products.Entities;
using EShop.Modules.Products.ValueObjects;

namespace EShop.Modules.Orders.Entities;

[Entity, Ordinal]
public partial class OrderLine
{
    public int ProductId { get; private init; }
    public uint Quantity { get; private init; }
    public Money UnitPrice { get; private init; }

    public static OrderLine Create(Product product, uint quantity, Money unitPrice) =>
        new() { ProductId = product.Id, Quantity = quantity, UnitPrice = unitPrice, Ordinal = 0 };
}
