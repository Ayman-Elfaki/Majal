using EShop.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace EShop.Modules.Customers.Endpoints;

/// <summary>
/// Lists all registered customers, reusing <see cref="RegisterCustomerCommand.CustomerDto"/> -- <c>Name</c>
/// and <c>Email</c> are both plain readable value-object properties, so no supplied arguments are needed.
/// </summary>
public class ListCustomersQuery
{
    [Tags("Customers")]
    [WolverineGet("/customers")]
    public static async Task<IResult> List([FromServices] EShopDbContext db, CancellationToken ct)
    {
        var customers = await db.Customers.ToListAsync(ct);

        var results = customers.Select(c => new
        {
            c.Id,
            Customer = RegisterCustomerCommand.CustomerDto.FromEntity(c)
        });

        return Results.Ok(results);
    }
}
