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
    [FlattenDtoFor<Money>]
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
            Product = p switch
            {
                DigitalProduct digitalProduct => new ProductDto.DigitalProductDto
                {
                    CategoryId = digitalProduct.Category.Id,
                    DownloadUrl = digitalProduct.DownloadUrl,
                    Sku = digitalProduct.Sku,
                    PriceAmount = digitalProduct.Price.Amount,
                    PriceCurrency = digitalProduct.Price.Currency,
                    Tags = digitalProduct.TagList.Values,
                    Translations = digitalProduct.Translations.Select(t => new ProductDto.ProductTranslationDto
                    {
                        Name = t.Name,
                        Locale = t.Locale,
                        Description = t.Description
                    }),
                    InitialStockQuantity = 0,
                },
                PhysicalProduct physicalProduct => new ProductDto.PhysicalProductDto
                {
                    WeightKg = physicalProduct.WeightKg,
                    Sku = physicalProduct.Sku,
                    PriceAmount = physicalProduct.Price.Amount,
                    PriceCurrency = physicalProduct.Price.Currency,
                    CategoryId = physicalProduct.Category.Id,
                    Tags = physicalProduct.TagList.Values,
                    Translations = physicalProduct.Translations
                        .Select(t => new ProductDto.ProductTranslationDto
                        {
                            Name = t.Name,
                            Locale = t.Locale,
                            Description = t.Description
                        }),
                    InitialStockQuantity = 0
                },
                _ => throw new ArgumentOutOfRangeException(nameof(p))
            },
        });

        return Results.Ok(results);
    }
}