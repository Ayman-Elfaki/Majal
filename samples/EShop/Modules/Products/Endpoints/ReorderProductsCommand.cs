using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace EShop.Modules.Products.Endpoints;

public class ReorderProductsCommand
{
    public record Request(IReadOnlyList<int> ProductIdsInOrder);

    [Tags("Products")]
    [WolverinePatch("/products/reorder")]
    public static async Task<IResult> Reorder(Request request, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        await db.Products.ReorderAsync(request.ProductIdsInOrder, ct);
        return Results.NoContent();
    }
}
