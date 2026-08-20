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

/// <summary>Create a new physical (shippable) product in an existing category.</summary>
public partial record CreatePhysicalProductCommand
{
    [DtoFor<PhysicalProduct>(Nullable = ["InitialStockQuantity"])]
    [FlattenDtoFor<Money>]
    public partial record PhysicalProductDto;

    public class Validator : AbstractValidator<PhysicalProductDto>
    {
        public Validator()
        {
            RuleFor(p => p.Sku).NotEmpty().MaximumLength(ProductSku.MaxLength);
            RuleFor(p => p.PriceAmount).GreaterThan(0);
            RuleFor(p => p.PriceCurrency).Length(3);
            RuleFor(p => p.WeightKg).GreaterThan(0);
            RuleFor(p => p.Translations).NotEmpty();
        }
    }

    [Tags("Catalog")]
    [WolverinePost("/products/physical")]
    public static async Task<IResult> Create(PhysicalProductDto dto, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        // CategoryId is aggregate-by-id, so ToEntity() isn't generated for this DTO -- look the category up
        // and call the domain factory directly, the same pattern the old Todo sample used.
        var category = await db.Categories.FindAsync([dto.CategoryId], ct);
        if (category is null) return Results.NotFound($"Category '{dto.CategoryId}' not found.");

        var product = PhysicalProduct.Create(
            ProductSku.Create(dto.Sku),
            Money.Create(dto.PriceAmount, dto.PriceCurrency),
            category,
            ProductTags.Create(dto.Tags),
            dto.Translations.Select(t => ProductTranslation.Create(t.Name, t.Description, t.Locale)),
            dto.WeightKg,
            dto.InitialStockQuantity ?? 0);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            product.Id,
            Product = PhysicalProductDto.FromEntity(product, dto.Tags, dto.Translations, product.StockQuantity)
        });
    }
}
