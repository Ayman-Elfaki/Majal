using EShop.Modules.Products.Entities;
using EShop.Modules.Products.ValueObjects;
using EShop.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace EShop.Modules.Products.Endpoints;

/// <summary>
/// Create a new digital (download) product. Unlike <see cref="CreatePhysicalProductCommand"/>, its price
/// is left unflattened, so it becomes a nested <c>MoneyDto</c> object here.
/// </summary>
public partial record CreateDigitalProductCommand
{
    [DtoFor<DigitalProduct>(Directions = MapDirection.ToEntity)]
    public partial record DigitalProductDtos;

    public class Validator : AbstractValidator<DigitalProductDtos>
    {
        public Validator()
        {
            RuleFor(p => p.Sku).NotEmpty().MaximumLength(ProductSku.MaxLength);
            RuleFor(p => p.DownloadUrl).NotEmpty();
            RuleFor(p => p.Translations).NotEmpty();
        }
    }

    [Tags("Products")]
    [WolverinePost("/products/digital")]
    public static async Task<IResult> Create(DigitalProductDtos dtos, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        var category = await db.Categories.FindAsync([dtos.CategoryId], ct);
        if (category is null) return Results.NotFound($"Category '{dtos.CategoryId}' not found.");
        
        var product = DigitalProduct.Create(
            ProductSku.Create(dtos.Sku),
            Money.Create(dtos.Price.Amount, dtos.Price.Currency),
            category,
            ProductTags.Create(dtos.Tags),
            dtos.Translations.Select(t => ProductTranslation.Create(t.Name, t.Description, t.Locale)),
            dtos.DownloadUrl,
            dtos.InitialStockQuantity);

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new
        {
            product.Id,
            Product = product
        });
    }
}
