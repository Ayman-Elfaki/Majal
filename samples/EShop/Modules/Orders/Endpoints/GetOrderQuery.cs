using EShop.Modules.Orders.Entities;
using EShop.Modules.Products.ValueObjects;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Orders.Endpoints;

/// <summary>Reads back a placed order.</summary>
public partial record GetOrderQuery
{
    [DtoFor<Order>]
    [FlattenDtoFor<Money>]
    public partial record OrderDto;

    public class ResponseDto
    {
        public Guid Id { get; set; }
        public IEnumerable<int> LineIds { get; set; } = [];
        public required OrderDto Order { get; set; }
    }

    [Tags("Orders")]
    [WolverineGet("/orders/{id:guid}")]
    public static async Task<IResult> Get(Guid id, [FromServices] EShopDbContext db, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Payment)
            .Include(o => o.LineItems)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return Results.NotFound();

        return Results.Ok(new ResponseDto
        {
            Id = order.Id,
            LineIds = order.LineItems.Select(l => l.Id),
            Order = new OrderDto
            {
                PaymentMethod = order.Payment switch
                {
                    CreditCardPayment p => new OrderDto.CreditCardPaymentDto
                    {
                        CardholderName = p.CardholderName,
                        Last4Digits = p.Last4Digits
                    },
                    PayPalPayment p => new OrderDto.PayPalPaymentDto
                    {
                        PayerEmail = p.PayerEmail
                    },
                    _ => throw new ArgumentOutOfRangeException()
                },
                CustomerId = order.CustomerId,
                Lines = order.LineItems.Select(l => new OrderDto.OrderLineDto
                {
                    Quantity = l.Quantity,
                    ProductId = l.ProductId,
                    UnitPriceAmount = l.UnitPrice.Amount,
                    UnitPriceCurrency = l.UnitPrice.Currency
                })
            }
        });
    }
}