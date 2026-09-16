namespace ReservationService.DTOs;

public class ReservationStatisticsDto
{
    public Guid UserId { get; set; }

    /// <summary>Count of reservations with status Reserved or CheckedOut.</summary>
    public int ActiveReservations { get; set; }

    /// <summary>Total count of all reservations ever created for this user.</summary>
    public int BorrowingHistory { get; set; }
}