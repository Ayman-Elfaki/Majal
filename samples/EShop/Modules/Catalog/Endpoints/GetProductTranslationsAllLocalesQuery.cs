using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using ProductTranslation = EShop.Modules.Catalog.Entities.ProductTranslation;

namespace EShop.Modules.Catalog.Endpoints;

/// <summary>Lists product translations across every locale, bypassing the default Translatable query filter.</summary>
public partial record GetProductTranslationsAllLocalesQuery
{
    [DtoFor<ProductTranslation>]
    public partial record ProductTranslationDto;

    [Tags("Catalog")]
    [WolverineGet("/admin/products/translations")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var translations = await db.Set<ProductTranslation>()
            .IgnoreTranslatableFilter()
            .ToListAsync(ct);

        return Results.Ok(translations);
    }
}
