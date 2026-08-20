using EShop.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Category = EShop.Modules.Catalog.Entities.Category;
using CategoryName = EShop.Modules.Catalog.ValueObjects.CategoryName;

namespace EShop.Modules.Catalog.Endpoints;

/// <summary>Create a new product category.</summary>
public partial record CreateCategoryCommand
{
    [DtoFor<Category>]
    public partial record CategoryDto;

    public class Validator : AbstractValidator<CategoryDto>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(CategoryName.MaxLength);
            RuleForEach(c => c.Translations).ChildRules(t =>
            {
                t.RuleFor(x => x.Description).NotEmpty();
                t.RuleFor(x => x.Locale).NotEmpty();
            });
        }
    }

    [Tags("Catalog")]
    [WolverinePost("/categories")]
    public static async Task<IResult> Create(CategoryDto dto, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        var category = dto.ToEntity();
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        // Id isn't a factory-method parameter, so [DtoFor] never includes it -- surface it alongside the
        // DTO so callers can reference what they just created.
        return Results.Ok(new { category.Id, Category = CategoryDto.FromEntity(category, dto.Translations) });
    }
}
