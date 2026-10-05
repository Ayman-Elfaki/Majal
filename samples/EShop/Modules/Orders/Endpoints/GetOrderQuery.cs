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
    [DtoInclude(nameof(Order.Id), nameof(Order.CreatedOn))]
    [DtoFlatten<Money>]
    [DtoMember("Lines", MapFrom = "LineItems")]
    [DtoMember("PaymentMethod", MapFrom = "Payment")]
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
            Order = OrderDto.FromEntity(order)
        });
    }
}