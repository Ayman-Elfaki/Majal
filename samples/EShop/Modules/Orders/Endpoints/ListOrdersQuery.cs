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
    [DtoInclude(nameof(Order.Id), nameof(Order.CreatedOn))]
    [DtoMember("Lines", MapFrom = "LineItems")]
    [DtoMember("PaymentMethod", MapFrom = "Payment")]
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
        
        var results = orders.OrderByDescending(o => o.CreatedOn).Select(o => new ResponseDto
        {
            Id = o.Id,
            CreatedOn = o.CreatedOn,
            LineIds = o.LineItems.Select(l => l.Id),
            Order = OrderDto.FromEntity(o)
        });

        return Results.Ok(results);
    }
}