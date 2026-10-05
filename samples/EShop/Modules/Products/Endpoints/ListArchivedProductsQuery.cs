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
/// <c>[DtoIgnoreType&lt;ProductTranslation&gt;]</c> since this listing doesn't need translations (see
/// <see cref="ListCategoriesQuery"/> for the <c>Prefix</c>-override demonstration).
/// </summary>
public partial record ListArchivedProductsQuery
{
    [DtoFor<Product>(Prefix = "Archived")]
    [DtoInclude(nameof(Product.Id))]
    [DtoFlatten<Money>]
    [DtoIgnoreType<ProductTranslation>]
    [DtoMember("InitialStockQuantity", MapFrom = "StockQuantity")]
    [DtoMember("Tags", MapFrom = "TagList.Values")]
    public partial record ProductDto;


    [Tags("Products")]
    [WolverineGet("/admin/products/archived")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var results = await db.Products
            .AsNoTracking()
            .AsSplitQuery()
            .IgnoreArchivableFilter()
            .Select(ProductDto.Projection)
            .ToListAsync(ct);

        return Results.Ok(results);
    }
}