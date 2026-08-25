using EShop.Modules.Categories.Entities;
using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Categories.Endpoints;

/// <summary>
/// Lists all categories (callers need this to discover a valid <c>categoryId</c> before creating a
/// product). Uses a <c>Prefix</c> override, visible in its nested <c>AdminCategoryTranslationDto</c> name
/// -- contrast with the default, unprefixed <c>CategoryTranslationDto</c> nested inside
/// <see cref="CreateCategoryCommand"/>.
/// </summary>
public partial record ListCategoriesQuery
{
    [DtoFor<Category>(Prefix = "Admin")]
    public partial class AdminCategoryDto
    {
        public int Id { get; set; }
    }

    [Tags("Categories")]
    [WolverineGet("/categories")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var categories = await db.Categories.Include(c => c.Translations).ToListAsync(ct);

        var results = categories.Select(c => new
        {
            c.Id,
            Category = new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Translations = c.Translations.Select(t => new AdminCategoryDto.AdminCategoryTranslationDto
                {
                    Locale = t.Locale,
                    Description = t.Description
                })
            }
        });

        return Results.Ok(results);
    }
}