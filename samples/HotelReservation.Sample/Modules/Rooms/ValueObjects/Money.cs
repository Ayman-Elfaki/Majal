namespace HotelReservation.Sample.Modules.Rooms.ValueObjects;

/// <summary>
/// The money value object
/// </summary>
[ValueObject]
public partial class Money
{
    /// <summary>
    /// The monetary amount
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// The three-letter ISO currency code
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Create Money
    /// </summary>
    /// <param name="amount">The monetary amount</param>
    /// <param name="currency">The three-letter ISO currency code</param>
    /// <returns>The created money value</returns>
    public static Money Create(decimal amount, string currency)
    {
        return new Money
        {
            Amount = amount,
            Currency = currency
        };
    }

    private IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
