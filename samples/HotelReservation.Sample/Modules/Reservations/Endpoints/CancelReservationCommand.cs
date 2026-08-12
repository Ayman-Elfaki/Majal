using HotelReservation.Sample.Common.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wolverine.Http;

namespace HotelReservation.Sample.Modules.Reservations.Endpoints;

/// <summary>
/// cancel an existing reservation
/// </summary>
public class CancelReservationCommand
{
    /// <summary>
    /// Cancel a reservation
    /// </summary>
    /// <param name="id"></param>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [Tags("Reservations")]
    [WolverinePost("/reservations/{id:int}/cancel")]
    public static async Task<IResult> Cancel(int id, [FromServices] AppDbContext context, CancellationToken ct)
    {
        var reservation = await context.Reservations.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (reservation is null) return Results.NotFound();

        reservation.Cancel();
        await context.SaveChangesAsync(ct);

        return Results.Ok();
    }
}
