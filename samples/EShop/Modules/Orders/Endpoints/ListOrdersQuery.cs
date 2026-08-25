using EShop.Modules.Orders.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Orders.Endpoints;

/// <summary>
/// Lists all orders, newest first, reusing <see cref="GetOrderQuery.OrderDto"/> and its nested polymorphic
/// payment DTOs rather than declaring a second <c>[DtoFor&lt;Order&gt;]</c>.
/// </summary>
public partial class ListOrdersQuery
{
    [DtoFor<Order>]
    public partial class OrderDto;

    public class ResponseDto
    {
        public Guid Id { get; set; }
        public IEnumerable<int> LineIds { get; set; } = [];
        public DateTimeOffset CreatedOn { get; set; }
        public required OrderDto Order { get; set; }
    }

    [Tags("Orders")]
    [WolverineGet("/orders")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var orders = await db.Orders.Include(o => o.LineItems).Include(o => o.Payment).ToListAsync(ct);

        var results = orders.OrderByDescending(o => o.CreatedOn).Select(o =>
        {
            return new ResponseDto
            {
                Id = o.Id,
                CreatedOn = o.CreatedOn,
                LineIds = o.LineItems.Select(l => l.Id),
                Order = new OrderDto
                {
                    CustomerId = o.CustomerId,
                    PaymentMethod = o.Payment switch
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
                    Lines = o.LineItems.Select(l => new OrderDto.OrderLineDto
                    {
                        ProductId = l.ProductId,
                        Quantity = l.Quantity,
                        UnitPrice = new OrderDto.MoneyDto
                        {
                            Amount = l.UnitPrice.Amount,
                            Currency = l.UnitPrice.Currency
                        }
                    })
                }
            };
        });

        return Results.Ok(results);
    }
}