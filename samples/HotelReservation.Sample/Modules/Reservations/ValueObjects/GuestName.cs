namespace HotelReservation.Sample.Modules.Reservations.ValueObjects;

/// <summary>
/// The guest name value object
/// </summary>
[ValueObject<string>]
public readonly partial struct GuestName
{
    internal const int MaxLength = 200;
}
