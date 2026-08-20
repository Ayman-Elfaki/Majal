using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;
using Category = EShop.Modules.Catalog.Entities.Category;

namespace EShop.Modules.Catalog.Endpoints;

/// <summary>
/// Lists all categories (callers need this to discover a valid <c>categoryId</c> before creating a
/// product). Uses a <c>Prefix</c> override, visible in its nested <c>AdminCategoryTranslationDto</c> name
/// -- contrast with the default, unprefixed <c>CategoryTranslationDto</c> nested inside
/// <see cref="CreateCategoryCommand"/>.
/// </summary>
public partial record GetCategoriesQuery
{
    [DtoFor<Category>(Prefix = "Admin")]
    public partial record AdminCategoryDto;

    [Tags("Catalog")]
    [WolverineGet("/categories")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.Include(c => c.TranslationList).ToListAsync(ct);

        var results = categories.Select(c => new
        {
            c.Id,
            Category = c
        });

        return Results.Ok(results);
    }
}
