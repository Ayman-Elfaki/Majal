using EShop.Modules.Categories.Endpoints;
using EShop.Modules.Products.Entities;
using EShop.Modules.Products.ValueObjects;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Products.Endpoints;

/// <summary>
/// Lists discontinued (archived) products, bypassing the default Archivable query filter. A single
/// <c>[DtoFor&lt;Product&gt;(Prefix = "Archived")]</c> on the abstract <see cref="Product"/> base generates
/// an abstract <c>ProductDto</c> plus one DTO per concrete subclass (<c>ArchivedDigitalProductDto</c>,
/// <c>ArchivedPhysicalProductDto</c>) in a single declaration. Its DTOs use a whole-type
/// <c>[ExcludeDtoFor&lt;ProductTranslation&gt;]</c> since this listing doesn't need translations (see
/// <see cref="ListCategoriesQuery"/> for the <c>Prefix</c>-override demonstration).
/// </summary>
public partial record ListArchivedProductsQuery
{
    [DtoFor<Product>(Prefix = "Archived")]
    [FlattenDtoFor<Money>]
    [ExcludeDtoFor<ProductTranslation>]
    public partial record ProductDto;


    [Tags("Products")]
    [WolverineGet("/admin/products/archived")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var physical = await db.Products.OfType<PhysicalProduct>()
            .AsNoTracking()
            .AsSplitQuery()
            .IgnoreArchivableFilter().Where(p => p.IsArchived)
            .Include(p => p.Category)
            .Select(p => new ProductDto.ArchivedPhysicalProductDto
            {
                CategoryId = p.Category.Id,
                Sku = p.Sku,
                PriceAmount = p.Price.Amount,
                PriceCurrency = p.Price.Currency,
                Tags = p.TagList.Values,
                WeightKg = p.WeightKg,
                InitialStockQuantity = p.StockQuantity,
            }).ToListAsync(ct);

        var digital = await db.Products.OfType<DigitalProduct>()
            .AsNoTracking()
            .AsSplitQuery()
            .IgnoreArchivableFilter().Where(p => p.IsArchived)
            .Include(p => p.Category)
            .Select(d => new ProductDto.ArchivedDigitalProductDto
            {
                Sku = d.Sku,
                CategoryId = d.Category.Id,
                PriceAmount = d.Price.Amount,
                PriceCurrency = d.Price.Currency,
                Tags = d.TagList.Values,
                DownloadUrl = d.DownloadUrl,
                InitialStockQuantity = d.StockQuantity
            }).ToListAsync(ct);

        var results = physical.Concat<ProductDto>(digital);

        return Results.Ok(results);
    }
}