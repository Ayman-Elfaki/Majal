using HotelReservation.Sample.Modules.Hotels.Entities;
using HotelReservation.Sample.Modules.Rooms.ValueObjects;

namespace HotelReservation.Sample.Modules.Rooms.Entities;

/// <summary>
/// Room entity
/// </summary>
[Entity, Aggregate]
[Auditable]
public partial class Room
{
    /// <summary>
    /// The room number
    /// </summary>
    public required RoomNumber Number { get; set; }

    /// <summary>
    /// The type of the room
    /// </summary>
    public required RoomType Type { get; set; }

    /// <summary>
    /// The nightly price amount
    /// </summary>
    public required decimal PriceAmount { get; set; }

    /// <summary>
    /// The nightly price currency
    /// </summary>
    public required string PriceCurrency { get; set; }

    /// <summary>
    /// The hotel the room belongs to
    /// </summary>
    public Hotel Hotel { get; set; } = null!;

    /// <summary>
    /// Create a room
    /// </summary>
    /// <param name="number">The room number</param>
    /// <param name="type">The type of the room</param>
    /// <param name="pricePerNight">The nightly price of the room</param>
    /// <param name="hotel">The hotel the room belongs to</param>
    /// <returns>The created room</returns>
    public static Room Create(RoomNumber number, RoomType type, Money pricePerNight, Hotel hotel)
    {
        return new Room
        {
            Number = number,
            Type = type,
            PriceAmount = pricePerNight.Amount,
            PriceCurrency = pricePerNight.Currency,
            Hotel = hotel
        };
    }
}
