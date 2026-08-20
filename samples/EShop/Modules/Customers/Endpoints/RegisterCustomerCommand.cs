using EShop.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Wolverine.Http;
using Customer = EShop.Modules.Customers.Entities.Customer;
using CustomerEmail = EShop.Modules.Customers.ValueObjects.CustomerEmail;
using CustomerName = EShop.Modules.Customers.ValueObjects.CustomerName;

namespace EShop.Modules.Customers.Endpoints;

/// <summary>Register a new customer.</summary>
public partial record RegisterCustomerCommand
{
    [DtoFor<Customer>]
    public partial record CustomerDto;

    public class Validator : AbstractValidator<CustomerDto>
    {
        public Validator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(CustomerName.MaxLength);
            RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(CustomerEmail.MaxLength);
        }
    }

    [Tags("Customers")]
    [WolverinePost("/customers")]
    public static async Task<IResult> Register(CustomerDto dto, [FromServices] EShopDbContext db,
        CancellationToken ct)
    {
        var customer = dto.ToEntity();
        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return Results.Ok(new { customer.Id, Customer = CustomerDto.FromEntity(customer) });
    }
}
