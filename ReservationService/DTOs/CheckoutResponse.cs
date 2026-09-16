using ReservationService.Enums;
namespace ReservationService.DTOs;

public class CheckoutResponse
{
    public Guid ReservationId { get; set; }
    public ReservationStatus Status { get; set; }
    public DateTime CheckedOutAt { get; set; }
    public DateTime DueDate { get; set; }
    public string Message { get; set; } = string.Empty;
}