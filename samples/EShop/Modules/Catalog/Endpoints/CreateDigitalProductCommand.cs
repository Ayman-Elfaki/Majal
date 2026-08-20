using EShop.Modules.Catalog.Entities;
using EShop.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Money = EShop.Modules.Catalog.ValueObjects.Money;
using ProductSku = EShop.Modules.Catalog.ValueObjects.ProductSku;
using ProductTags = EShop.Modules.Catalog.ValueObjects.ProductTags;
using ProductTranslation = EShop.Modules.Catalog.Entities.ProductTranslation;

namespace EShop.Modules.Catalog.Endpoints;

/// <summary>
/// Create a new digital (download) product. Unlike <see cref="CreatePhysicalProductCommand"/>, its price
/// is left unflattened, so it becomes a nested <c>MoneyDto</c> object here.
/// </summary>
public partial record CreateDigitalProductCommand
{
    [DtoFor<DigitalProduct>]
    public partial record DigitalProductDto;

    public class Validator : AbstractValidator<DigitalProductDto>
    {
        public Validator()
        {
            RuleFor(p => p.Sku).NotEmpty().MaximumLength(ProductSku.MaxLength);
            RuleFor(p => p.DownloadUrl).NotEmpty();
            RuleFor(p => p.Translations).NotEmpty();
        }
    }

    [Tags("Catalog")]
    [WolverinePost("/products/digital")]
    public static async Task<IResult> Create(DigitalProductDto dto, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([dto.CategoryId], ct);
        if (category is null) return Results.NotFound($"Category '{dto.CategoryId}' not found.");

        var product = DigitalProduct.Create(
            ProductSku.Create(dto.Sku),
            Money.Create(dto.Price.Amount, dto.Price.Currency),
            category,
            ProductTags.Create(dto.Tags),
            dto.Translations.Select(t => ProductTranslation.Create(t.Name, t.Description, t.Locale)),
            dto.DownloadUrl,
            dto.InitialStockQuantity);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            product.Id,
            Product = product
        });
    }
}
