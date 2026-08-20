using EShop.Modules.Orders.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using Order = EShop.Modules.Orders.Entities.Order;

namespace EShop.Modules.Orders.Endpoints;

/// <summary>Reads back a placed order, demonstrating the FromEntity() read path for multi-level nesting.</summary>
public partial record GetOrderQuery
{
    [DtoFor<Order>]
    public partial record OrderDto;

    [Tags("Orders")]
    [WolverineGet("/orders/{id:guid}")]
    public static async Task<IResult> Get(Guid id, [FromServices] EShopDbContext db, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Payment)
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return Results.NotFound();

        var lines = order.LineItems.Select(lineItem =>
            OrderDto.OrderLineDto.FromEntity(
                lineItem,
                lineItem.ProductId,
                OrderDto.MoneyDto.FromEntity(lineItem.UnitPrice)
            )
        );

        var payment = order.Payment switch
        {
            CreditCardPayment creditCard => (object)OrderDto.CreditCardPaymentDto.FromEntity(creditCard),
            PayPalPayment payPal => OrderDto.PayPalPaymentDto.FromEntity(payPal),
            _ => throw new InvalidOperationException($"Unknown payment method '{order.Payment.GetType()}'.")
        };

        return Results.Ok(new
        {
            order.Id,
            LineIds = order.LineItems.Select(l => l.Id),
            Order = OrderDto.FromEntity(order, order.CustomerId, lines, (OrderDto.PaymentMethodDto)payment)
        });
    }
}
