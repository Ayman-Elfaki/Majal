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
public class GetOrdersQuery
{
    [Tags("Orders")]
    [WolverineGet("/orders")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        // SQLite can't translate ORDER BY on a DateTimeOffset column, so sort after materializing.
        var orders = await db.Orders.Include(o => o.LineItems).Include(o => o.Payment).ToListAsync(ct);

        var results = orders.OrderByDescending(o => o.CreatedOn).Select(order =>
        {
            return new
            {
                order.Id,
                order.CreatedOn,
                LineIds = order.LineItems.Select(l => l.Id),
                Order = order
            };
        });

        return Results.Ok(results);
    }
}
