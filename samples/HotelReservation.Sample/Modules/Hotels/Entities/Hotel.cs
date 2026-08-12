using HotelReservation.Sample.Common.Extensions;
using HotelReservation.Sample.Modules.Hotels.ValueObjects;
using HotelReservation.Sample.Modules.Rooms.Entities;

namespace HotelReservation.Sample.Modules.Hotels.Entities;

/// <summary>
/// Hotel entity
/// </summary>
[Entity, Aggregate]
[Archivable, Auditable, Ordinal]
public partial class Hotel
{
    /// <summary>
    /// The name of the hotel
    /// </summary>
    public required HotelName Name { get; init; }

    /// <summary>
    /// The rooms belonging to the hotel
    /// </summary>
    public ICollection<Room> Rooms { get; set; } = [];

    /// <summary>
    /// The translations of the hotel
    /// </summary>
    public ICollection<HotelTranslation> Translations { get; protected set; } = [];

    /// <summary>
    /// Create a hotel
    /// </summary>
    /// <param name="name">The name of the hotel</param>
    /// <param name="translations">The translations for the hotel</param>
    /// <returns>The created hotel</returns>
    /// <exception cref="ArgumentException">The translation must include all required locales.</exception>
    public static Hotel Create(HotelName name, HotelTranslation[] translations)
    {
        if (!translations.HasRequiredLocales())
            throw new ArgumentException("translation must include all required locales.");

        return new Hotel
        {
            Ordinal = 1,
            Name = name,
            Translations = [.. translations]
        };
    }
}
