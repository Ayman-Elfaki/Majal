using EShop.Common.Validators;
using EShop.Modules.Categories.Entities;
using EShop.Modules.Categories.ValueObjects;
using EShop.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;

namespace EShop.Modules.Categories.Endpoints;

/// <summary>Create a new product category.</summary>
public partial record CreateCategoryCommand
{
    [DtoFor<Category>]
    public partial record CategoryDtos;

    public class Validator : AbstractValidator<CategoryDtos>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(CategoryName.MaxLength);

            RuleFor(c => c.Translations).SetValidator(new TranslatableValidator());
            
            RuleForEach(c => c.Translations).ChildRules(t =>
            {
                t.RuleFor(x => x.Description).NotEmpty();
                t.RuleFor(x => x.Locale).NotEmpty();
            });
        }
    }

    [Tags("Categories")]
    [WolverinePost("/categories")]
    public static async Task<IResult> Create(CategoryDtos dtos, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        var category = dtos.ToEntity();
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        // Id isn't a factory-method parameter, so [DtoFor] never includes it -- surface it alongside the
        // DTO so callers can reference what they just created.
        return Results.Created();
    }
}
