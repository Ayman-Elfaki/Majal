using EShop.Modules.Catalog.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Catalog.Endpoints;

public class ReorderProductsCommand
{
    public record Request(IReadOnlyList<int> ProductIdsInOrder);

    [Tags("Catalog")]
    [WolverinePatch("/products/reorder")]
    public static async Task<IResult> Reorder(Request request, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        var products = await db.Products
            .Where(p => request.ProductIdsInOrder.Contains(p.Id))
            .ToListAsync(ct);

        var orderedProducts = request.ProductIdsInOrder
            .Select(id => products.FirstOrDefault(p => p.Id == id))
            .OfType<Product>()
            .ToList();
            
        Product.Reorder(orderedProducts);

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
