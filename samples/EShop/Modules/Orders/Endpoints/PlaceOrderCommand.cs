using EShop.Modules.Orders.Entities;
using EShop.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.Http;
using Money = EShop.Modules.Catalog.ValueObjects.Money;
using Order = EShop.Modules.Orders.Entities.Order;
using OrderLine = EShop.Modules.Orders.Entities.OrderLine;
using PaymentMethod = EShop.Modules.Orders.Entities.PaymentMethod;

namespace EShop.Modules.Orders.Endpoints;

/// <summary>
/// Place a new order. <c>PaymentMethod</c> is abstract with two derived factory methods (see
/// <see cref="CreditCardPayment"/>/<see cref="PayPalPayment"/>),
/// so it becomes a polymorphic nested DTO here, and the unit price uses a reversed flatten, propagating
/// into the nested <c>OrderLineDto</c> as <c>AmountUnitPrice</c>/<c>CurrencyUnitPrice</c>.
/// </summary>
public partial record PlaceOrderCommand
{
    [DtoFor<Order>]
    [FlattenDtoFor<Money>(IsReversed = true)]
    public partial record OrderDto;

    public class Validator : AbstractValidator<OrderDto>
    {
        public Validator()
        {
            RuleFor(o => o.Lines).NotEmpty();
            RuleForEach(o => o.Lines).ChildRules(l =>
            {
                l.RuleFor(x => x.Quantity).GreaterThan(0u);
                l.RuleFor(x => x.AmountUnitPrice).GreaterThan(0);
                l.RuleFor(x => x.CurrencyUnitPrice).Length(3);
            });
        }
    }

    [Tags("Orders")]
    [WolverinePost("/orders")]
    public static async Task<IResult> Place(OrderDto dto, [FromServices] EShopDbContext db,
        [FromServices] IMessageBus bus, CancellationToken ct)
    {
        var customer = await db.Customers.FindAsync([dto.CustomerId], ct);
        if (customer is null) return Results.NotFound($"Customer '{dto.CustomerId}' not found.");

        var productIds = dto.Lines.Select(l => l.ProductId).Distinct().ToArray();
        var products = await db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        if (products.Count != productIds.Length) return Results.NotFound("One or more products were not found.");

        var lines = dto.Lines.Select(line =>
            OrderLine.Create(
                products[line.ProductId], 
                line.Quantity, 
                Money.Create(line.AmountUnitPrice, 
                line.CurrencyUnitPrice)
            )
        );

        // dto.PaymentMethod is the abstract polymorphic base type at compile time, but the actual instance
        // deserialized is always one of the concrete derived DTOs, each with its own generated ToEntity().
        PaymentMethod paymentMethod = dto.PaymentMethod switch
        {
            OrderDto.CreditCardPaymentDto creditCard => creditCard.ToEntity(),
            OrderDto.PayPalPaymentDto payPal => payPal.ToEntity(),
            _ => throw new InvalidOperationException(
                $"Unknown payment method DTO '{dto.PaymentMethod.GetType()}'.")
        };

        var order = Order.Create(customer, [.. lines], paymentMethod);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        foreach (var domainEvent in order.Events) await bus.PublishAsync(domainEvent);
        order.Clear();

        return Results.Ok(new
        {
            order.Id,
            Order = OrderDto.FromEntity(order, dto.CustomerId, dto.Lines, dto.PaymentMethod)
        });
    }
}
