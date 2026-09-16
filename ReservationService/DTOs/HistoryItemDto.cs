using ReservationService.Enums;

namespace ReservationService.DTOs;

public class HistoryItemDto
{
    public Guid ReservationId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public DateTime ReservedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public int? LateDays { get; set; }
    public decimal? LateFee { get; set; }
    public bool WasLate { get; set; }
}