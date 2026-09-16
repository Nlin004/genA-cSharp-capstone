namespace UserService.DTOs;

public class ReservationStatisticsDto
{
    public Guid UserId { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}