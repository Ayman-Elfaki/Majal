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

        // "locale" isn't a readable property the DTO generator can see (Locale is added by a separate
        // generator pass), so FromEntity() takes it as a supplied argument -- see docs/dtos.md.
        return Results.Ok(translations.Select(t => ProductTranslationDto.FromEntity(t, t.Locale.Name)));
    }
}
