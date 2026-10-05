using EShop.Modules.Products.Entities;
using EShop.Modules.Products.ValueObjects;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Products.Endpoints;

/// <summary>
/// Lists active, in-stock-ordered products. <see cref="Product"/> is abstract; while
/// <c>[DtoFor&lt;Product&gt;]</c> can now generate a polymorphic subclass hierarchy directly (see
/// <see cref="ListArchivedProductsQuery"/>), this endpoint still composes per-subtype DTOs manually --
/// it reuses the already-generated <c>PhysicalProductDto</c>/<c>DigitalProductDto</c> from the create
/// commands, and layers the Auditable/Ordinal fields on top since those aren't factory-method parameters
/// the generator would ever see, and it needs a single <c>Ordinal</c>-ordered query across both subtypes
/// that a two-query-then-concat DTO projection can't preserve.
/// </summary>
public partial class GetProductsQuery
{
    [DtoFor<Product>]
    [DtoFlatten<Money>]
    [DtoMember("Tags", MapFrom = "TagList.Values")]
    [DtoMember("InitialStockQuantity", MapFrom = "StockQuantity")]
    public partial class ProductDto;

    public class ResponseDto
    {
        public int Id { get; init; }
        public uint Ordinal { get; init; }
        public required ProductDto Product { get; init; }
        public DateTimeOffset CreatedOn { get; init; }
        public DateTimeOffset? UpdatedOn { get; init; }
    }

    [Tags("Products")]
    [WolverineGet("/products")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var products = await db.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Category)
            .Include(p => p.Translations)
            .OrderBy(p => p.Ordinal)
            .ToListAsync(ct);

        var results = products.Select(p => new ResponseDto
        {
            Id = p.Id,
            Ordinal = p.Ordinal,
            CreatedOn = p.CreatedOn,
            UpdatedOn = p.UpdatedOn,
            Product = ProductDto.FromEntity(p)
        });

        return Results.Ok(results);
    }
}