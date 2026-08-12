using HotelReservation.Sample.Modules.Reservations.ValueObjects;
using HotelReservation.Sample.Modules.Rooms.Entities;

namespace HotelReservation.Sample.Modules.Reservations.Entities;

/// <summary>
/// Reservation entity
/// </summary>
[Entity, Aggregate]
[Auditable]
public partial class Reservation
{
    /// <summary>
    /// The name of the guest
    /// </summary>
    public required GuestName GuestName { get; set; }

    /// <summary>
    /// The email of the guest
    /// </summary>
    public required GuestEmail GuestEmail { get; set; }

    /// <summary>
    /// The check-in date
    /// </summary>
    public required DateOnly CheckInDate { get; set; }

    /// <summary>
    /// The check-out date
    /// </summary>
    public required DateOnly CheckOutDate { get; set; }

    /// <summary>
    /// The total price amount for the stay
    /// </summary>
    public required decimal TotalPriceAmount { get; set; }

    /// <summary>
    /// The total price currency for the stay
    /// </summary>
    public required string TotalPriceCurrency { get; set; }

    /// <summary>
    /// The status of the reservation
    /// </summary>
    public required ReservationStatus Status { get; set; }

    /// <summary>
    /// The reserved room
    /// </summary>
    public Room Room { get; set; } = null!;

    /// <summary>
    /// Create a reservation
    /// </summary>
    /// <param name="guestName">The name of the guest</param>
    /// <param name="guestEmail">The email of the guest</param>
    /// <param name="checkInDate">The check-in date</param>
    /// <param name="checkOutDate">The check-out date</param>
    /// <param name="room">The room being reserved</param>
    /// <returns>The created reservation</returns>
    /// <exception cref="ArgumentException">The check-out date must be after the check-in date.</exception>
    public static Reservation Create(GuestName guestName, GuestEmail guestEmail, DateOnly checkInDate,
        DateOnly checkOutDate, Room room)
    {
        if (checkOutDate <= checkInDate)
            throw new ArgumentException("Check-out date must be after the check-in date.");

        var nights = checkOutDate.DayNumber - checkInDate.DayNumber;

        return new Reservation
        {
            GuestName = guestName,
            GuestEmail = guestEmail,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            TotalPriceAmount = room.PriceAmount * nights,
            TotalPriceCurrency = room.PriceCurrency,
            Status = ReservationStatus.Pending,
            Room = room
        };
    }

    /// <summary>
    /// Confirm the reservation
    /// </summary>
    public void Confirm() => Status = ReservationStatus.Confirmed;

    /// <summary>
    /// Cancel the reservation
    /// </summary>
    public void Cancel() => Status = ReservationStatus.Cancelled;
}
