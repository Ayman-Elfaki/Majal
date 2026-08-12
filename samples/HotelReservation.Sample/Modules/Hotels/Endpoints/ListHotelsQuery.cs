using HotelReservation.Sample.Common.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace HotelReservation.Sample.Modules.Hotels.Endpoints;

/// <summary>
/// List all hotels
/// </summary>
public class ListHotelsQuery
{
    /// <summary>
    /// The Query Response
    /// </summary>
    public class ListHotelsResponse
    {
        /// <summary>
        /// The hotels
        /// </summary>
        public IEnumerable<HotelDto> Hotels { get; set; } = [];
    }

    /// <summary>
    /// the dto for a room
    /// </summary>
    public class RoomDto
    {
        /// <summary>
        /// The room number
        /// </summary>
        public required string Number { get; set; }
        /// <summary>
        /// The room type
        /// </summary>
        public required string Type { get; set; }
        /// <summary>
        /// The nightly price amount
        /// </summary>
        public required decimal PriceAmount { get; set; }
        /// <summary>
        /// The nightly price currency
        /// </summary>
        public required string PriceCurrency { get; set; }
    }

    /// <summary>
    /// the dto for a hotel
    /// </summary>
    public class HotelDto
    {
        /// <summary>
        /// The locale
        /// </summary>
        public required string Locale { get; init; }
        /// <summary>
        /// The hotel name
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// The hotel display name
        /// </summary>
        public required string DisplayName { get; init; }
        /// <summary>
        /// The hotel description
        /// </summary>
        public required string Description { get; init; }
        /// <summary>
        /// The rooms
        /// </summary>
        public IEnumerable<RoomDto> Rooms { get; set; } = [];
    }

    /// <summary>
    /// List all hotels
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Hotels")]
    [WolverineGet("/hotels")]
    [ProducesResponseType<ListHotelsResponse>(200)]
    public static async Task<IResult> List([FromServices] AppDbContext context, CancellationToken ct)
    {
        var hotelsQuery = await context.Hotels
            .AsNoTracking()
            .AsSplitQuery()
            .Select(h => new { h.Name, h.Translations, h.Rooms })
            .ToListAsync(ct);

        var hotels =
            from hotel in hotelsQuery
            let translation = hotel.Translations.First()
            select new HotelDto
            {
                Name = hotel.Name,
                DisplayName = translation.DisplayName,
                Description = translation.Description,
                Locale = translation.Locale.ToString(),
                Rooms = hotel.Rooms.Select(r => new RoomDto
                {
                    Number = r.Number,
                    Type = r.Type.ToString(),
                    PriceAmount = r.PriceAmount,
                    PriceCurrency = r.PriceCurrency
                })
            };

        return Results.Ok(new ListHotelsResponse { Hotels = hotels });
    }
}
