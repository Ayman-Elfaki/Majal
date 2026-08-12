using FluentValidation;
using HotelReservation.Sample.Common.Persistence;
using HotelReservation.Sample.Modules.Reservations.Entities;
using HotelReservation.Sample.Modules.Reservations.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace HotelReservation.Sample.Modules.Reservations.Endpoints;

/// <summary>
/// create a new reservation
/// </summary>
public partial class CreateReservationCommand
{
    /// <summary>
    /// The Reservation Dto
    /// </summary>
    [DtoFor<Reservation>]
    public partial class ReservationDtos;




    /// <summary>
    /// The Dto Validator
    /// </summary>
    public class Validator : AbstractValidator<ReservationDtos>
    {
        /// <summary>
        /// the validator constructor
        /// </summary>
        public Validator()
        {
            RuleFor(dto => dto.GuestName)
                .NotEmpty()
                .MaximumLength(GuestName.MaxLength);

            RuleFor(dto => dto.GuestEmail)
                .NotEmpty()
                .MaximumLength(GuestEmail.MaxLength)
                .EmailAddress();

            RuleFor(dto => dto.CheckOutDate)
                .GreaterThan(dto => dto.CheckInDate);
        }
    }

    /// <summary>
    /// Create a new reservation
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Reservations")]
    [WolverinePost("/reservations")]
    public static async Task<IResult> Create(ReservationDtos dto, [FromServices] AppDbContext context,
        CancellationToken ct)
    {

        var room = await context.Rooms.FirstOrDefaultAsync(r => r.Id == dto.RoomId, ct);

        if (room is null) return Results.NotFound();

        var reservation = Reservation.Create(
            GuestName.Create(dto.GuestName),
            GuestEmail.Create(dto.GuestEmail),
            dto.CheckInDate,
            dto.CheckOutDate,
            room
        );

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync(ct);

        return Results.Ok();
    }
}
