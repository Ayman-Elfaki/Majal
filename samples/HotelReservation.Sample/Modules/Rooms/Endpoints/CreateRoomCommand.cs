using FluentValidation;
using HotelReservation.Sample.Common.Persistence;
using HotelReservation.Sample.Modules.Rooms.Entities;
using HotelReservation.Sample.Modules.Rooms.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace HotelReservation.Sample.Modules.Rooms.Endpoints;

/// <summary>
/// add a new room to a hotel
/// </summary>
public partial class CreateRoomCommand
{
    /// <summary>
    /// The Room Dto
    /// </summary>
    [DtoFor<Room>]
    [FlattenDtoFor<Money>(IsReversed = true)]
    public partial class RoomDtos;

    /// <summary>
    /// The Dto Validator
    /// </summary>
    public class Validator : AbstractValidator<RoomDtos>
    {
        /// <summary>
        /// the validator constructor
        /// </summary>
        public Validator()
        {
            RuleFor(dto => dto.Number)
                .NotEmpty()
                .MaximumLength(RoomNumber.MaxLength);

            RuleFor(dto => dto.Type)
                .IsInEnum();

            RuleFor(dto => dto.AmountPricePerNight)
                .GreaterThan(0);

            RuleFor(dto => dto.CurrencyPricePerNight)
                .NotEmpty()
                .Length(3);
        }
    }

    /// <summary>
    /// Add a room to a hotel
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Rooms")]
    [WolverinePost("/rooms")]
    public static async Task<IResult> Create(RoomDtos dto, [FromServices] AppDbContext context, CancellationToken ct)
    {
        var hotel = await context.Hotels.FirstOrDefaultAsync(h => h.Id == dto.HotelId, ct);

        if (hotel is null) return Results.NotFound();

        var room = Room.Create(
            RoomNumber.Create(dto.Number),
            dto.Type,
            Money.Create(dto.AmountPricePerNight, dto.CurrencyPricePerNight),
            hotel
        );

        hotel.Rooms.Add(room);
        await context.SaveChangesAsync(ct);

        return Results.Ok();
    }
}
