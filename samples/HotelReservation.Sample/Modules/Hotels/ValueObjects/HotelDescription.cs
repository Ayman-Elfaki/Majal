namespace HotelReservation.Sample.Modules.Hotels.ValueObjects;

/// <summary>
/// The hotel description value object
/// </summary>
[ValueObject<string>]
public readonly partial struct HotelDescription
{
    internal const int MaxLength = 2048;
}
