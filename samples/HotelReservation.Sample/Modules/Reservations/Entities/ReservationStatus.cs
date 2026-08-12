namespace HotelReservation.Sample.Modules.Reservations.Entities;

/// <summary>
/// The status of a reservation
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// Awaiting confirmation
    /// </summary>
    Pending,

    /// <summary>
    /// Confirmed by the hotel
    /// </summary>
    Confirmed,

    /// <summary>
    /// Cancelled by the guest or the hotel
    /// </summary>
    Cancelled
}
