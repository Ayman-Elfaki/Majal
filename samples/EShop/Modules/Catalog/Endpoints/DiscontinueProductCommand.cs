using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace EShop.Modules.Catalog.Endpoints;

/// <summary>
/// Discontinues a product. <c>Remove</c> here is intercepted by Majal's ArchivableSaveChangesInterceptor
/// and turned into an <c>IsArchived = true</c> update rather than a real delete.
/// </summary>
public class DiscontinueProductCommand
{
    [Tags("Catalog")]
    [WolverineDelete("/products/{id:int}")]
    public static async Task<IResult> Discontinue(int id, [FromServices] EShopDbContext db, CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null) return Results.NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}
