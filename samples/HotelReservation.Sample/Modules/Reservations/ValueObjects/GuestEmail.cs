namespace HotelReservation.Sample.Modules.Reservations.ValueObjects;

/// <summary>
/// The guest email value object
/// </summary>
[ValueObject<string>]
public readonly partial struct GuestEmail
{
    internal const int MaxLength = 320;
}
