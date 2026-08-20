using EShop.Modules.Catalog.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using Money = EShop.Modules.Catalog.ValueObjects.Money;
using ProductTranslation = EShop.Modules.Catalog.Entities.ProductTranslation;

namespace EShop.Modules.Catalog.Endpoints;

/// <summary>
/// Lists discontinued (archived) products, bypassing the default Archivable query filter. Its DTOs use a
/// whole-type <c>[ExcludeDtoFor&lt;ProductTranslation&gt;]</c> since this listing doesn't need translations
/// (see <see cref="GetCategoriesQuery"/> for the <c>Prefix</c>-override demonstration).
/// </summary>
public partial record GetArchivedProductsQuery
{
    [DtoFor<PhysicalProduct>]
    [FlattenDtoFor<Money>]
    [ExcludeDtoFor<ProductTranslation>]
    public partial record ArchivedPhysicalProductDto;

    [DtoFor<DigitalProduct>]
    [ExcludeDtoFor<ProductTranslation>]
    public partial record ArchivedDigitalProductDto;

    [Tags("Catalog")]
    [WolverineGet("/admin/products/archived")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var physical = await db.Products.OfType<PhysicalProduct>()
            .IgnoreArchivableFilter()
            .Where(p => p.IsArchived)
            .Include(p => p.Category)
            .ToListAsync(ct);

        var digital = await db.Products.OfType<DigitalProduct>()
            .IgnoreArchivableFilter()
            .Where(p => p.IsArchived)
            .Include(p => p.Category)
            .ToListAsync(ct);

        var results = physical
            .Select(p => new
            {
                p.Id,
                Product = (object)p
            })
            .Concat(digital.Select(d => new
            {
                d.Id,
                Product = (object)d
            }));

        return Results.Ok(results);
    }
}
