using ReservationService.Enums;

namespace ReservationService.DTOs;

public class CreateReservationResponse
{
    public Guid ReservationId { get; set; }
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
}