using System.Globalization;
using HotelReservation.Sample.Common.Extensions;
using HotelReservation.Sample.Modules.Hotels.ValueObjects;

namespace HotelReservation.Sample.Modules.Hotels.Entities;

/// <summary>
/// The hotel translation entity
/// </summary>
[Entity]
[Translatable<CultureInfo>]
public partial class HotelTranslation
{
    /// <summary>
    /// The display name for the hotel
    /// </summary>
    public required HotelName DisplayName { get; set; }

    /// <summary>
    /// The description for the hotel
    /// </summary>
    public required HotelDescription Description { get; set; }

    /// <summary>
    /// Create a hotel translation
    /// </summary>
    /// <param name="displayName">The display name for the hotel</param>
    /// <param name="description">The description for the hotel</param>
    /// <param name="locale">The locale for the hotel translation</param>
    /// <returns>The created hotel translation</returns>
    /// <exception cref="NotSupportedException">Thrown if the locale is not supported</exception>
    public static HotelTranslation Create(HotelName displayName, HotelDescription description, string locale)
    {
        if (!locale.IsLocaleSupported())
            throw new NotSupportedException($"Language {locale} is not supported");

        return new HotelTranslation
        {
            DisplayName = displayName,
            Description = description,
            Locale = CultureInfo.GetCultureInfoByIetfLanguageTag(locale)
        };
    }
}
