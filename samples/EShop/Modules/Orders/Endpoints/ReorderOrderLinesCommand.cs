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
        var order = await db.Orders.Include(o => o.LineItems).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null) return Results.NotFound();

        for (var i = 0; i < request.LineIdsInOrder.Count; i++)
        {
            var line = order.LineItems.FirstOrDefault(l => l.Id == request.LineIdsInOrder[i]);
            if (line is not null) line.Ordinal = (uint)i;
        }

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
