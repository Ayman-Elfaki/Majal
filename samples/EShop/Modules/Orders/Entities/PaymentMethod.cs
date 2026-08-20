namespace EShop.Modules.Orders.Entities;

/// <summary>
/// Base payment method. Plain <c>[Entity]</c> (not an aggregate) with no factory method of its own and
/// two derived factory methods below, so referencing it as a nested factory parameter (see
/// <see cref="Order.Create"/>) triggers polymorphic DTO generation.
/// </summary>
[Entity]
public abstract partial class PaymentMethod;

public class CreditCardPayment : PaymentMethod
{
    public string CardholderName { get; private init; } = string.Empty;
    public string Last4Digits { get; private init; } = string.Empty;

    public static CreditCardPayment Create(string cardholderName, string last4Digits) =>
        new() { CardholderName = cardholderName, Last4Digits = last4Digits };
}

public class PayPalPayment : PaymentMethod
{
    public string PayerEmail { get; private init; } = string.Empty;

    public static PayPalPayment Create(string payerEmail) => new() { PayerEmail = payerEmail };
}
