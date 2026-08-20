using EShop.Modules.Catalog.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using Product = EShop.Modules.Catalog.Entities.Product;

namespace EShop.Modules.Catalog.Endpoints;

/// <summary>
/// Lists active, in-stock-ordered products. <see cref="Product"/> is abstract with no factory method of
/// its own, so it can't be a direct <c>[DtoFor]</c> target -- this reuses the already-generated
/// <c>PhysicalProductDto</c>/<c>DigitalProductDto</c> from the create commands instead, and layers the
/// Auditable/Ordinal fields on top manually since those aren't factory-method parameters the generator
/// would ever see.
/// </summary>
public class GetProductsQuery
{
    [Tags("Catalog")]
    [WolverineGet("/products")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var products = await db.Products
            .Include(p => p.Category)
            .Include(p => p.TranslationList)
            .OrderBy(p => p.Ordinal)
            .ToListAsync(ct);

        var results = products.Select(p => new
        {
            p.Id,
            Product = ToDto(p),
            p.CreatedOn,
            p.UpdatedOn,
            p.Ordinal
        });

        return Results.Ok(results);
    }

    private static object ToDto(Product product) => product switch
    {
        PhysicalProduct physical => CreatePhysicalProductCommand.PhysicalProductDto.FromEntity(
            physical,
            physical.TagList.Values,
            physical.TranslationList.Select(t =>
                CreatePhysicalProductCommand.PhysicalProductDto.ProductTranslationDto.FromEntity(t, t.Locale.Name)),
            physical.StockQuantity),
        DigitalProduct digital => CreateDigitalProductCommand.DigitalProductDto.FromEntity(
            digital,
            CreateDigitalProductCommand.DigitalProductDto.MoneyDto.FromEntity(digital.Price),
            digital.TagList.Values,
            digital.TranslationList.Select(t =>
                CreateDigitalProductCommand.DigitalProductDto.ProductTranslationDto.FromEntity(t, t.Locale.Name)),
            digital.StockQuantity),
        _ => throw new InvalidOperationException($"Unknown product type '{product.GetType()}'.")
    };
}
