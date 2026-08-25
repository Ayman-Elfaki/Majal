using EShop.Modules.Orders.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Orders.Endpoints;

/// <summary>A second <c>[Ordinal]</c> reordering demonstration, on a different entity than the product one.</summary>
public class ReorderOrderLinesCommand
{
    public record Request(IReadOnlyList<int> LineIdsInOrder);

    [Tags("Orders")]
    [WolverinePatch("/orders/{id:guid}/lines/reorder")]
    public static async Task<IResult> Reorder(Guid id, Request request, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        var exists = await db.Orders.AnyAsync(o => o.Id == id, ct);
        if (!exists) return Results.NotFound();

        await db.Set<OrderLine>().ReorderAsync(request.LineIdsInOrder, ct);
        return Results.NoContent();
    }
}
