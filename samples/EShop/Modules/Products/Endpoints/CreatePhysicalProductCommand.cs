using EShop.Modules.Products.Entities;
using EShop.Modules.Products.ValueObjects;
using EShop.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace EShop.Modules.Products.Endpoints;

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

    [Tags("Products")]
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
            Product = product
        });
    }
}
