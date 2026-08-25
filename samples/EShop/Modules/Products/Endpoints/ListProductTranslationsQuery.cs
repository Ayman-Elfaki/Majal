using EShop.Modules.Products.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Products.Endpoints;

/// <summary>Lists product translations across every locale, bypassing the default Translatable query filter.</summary>
public partial record ListProductTranslationsQuery
{
    [DtoFor<ProductTranslation>]
    public partial class ProductTranslationDto
    {
        public int Id { get; set; }
    }

    [Tags("Products")]
    [WolverineGet("/admin/products/translations")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var translations = await db.Set<ProductTranslation>()
            .IgnoreTranslatableFilter()
            .ToListAsync(ct);

        var result = translations.Select(t => new ProductTranslationDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Locale = t.Locale
        });

        return Results.Ok(result);
    }
}