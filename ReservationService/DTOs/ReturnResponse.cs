namespace ReservationService.DTOs;

public class ReturnResponse
{
    public Guid ReservationId { get; set; }
    public DateTime ReturnedAt { get; set; }
    public int LateDays { get; set; }
    public decimal LateFee { get; set; }
    public string Message { get; set; } = string.Empty;
}