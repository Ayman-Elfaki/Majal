namespace HotelReservation.Sample.Modules.Hotels.ValueObjects;

/// <summary>
/// The hotel name value object
/// </summary>
[ValueObject<string>]
public readonly partial struct HotelName
{
    internal const int MaxLength = 200;
}
