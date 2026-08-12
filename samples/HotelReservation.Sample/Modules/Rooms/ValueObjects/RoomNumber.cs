namespace HotelReservation.Sample.Modules.Rooms.ValueObjects;

/// <summary>
/// The room number value object
/// </summary>
[ValueObject<string>]
public readonly partial struct RoomNumber
{
    internal const int MaxLength = 10;
}
