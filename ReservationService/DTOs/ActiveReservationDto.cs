using ReservationService.Enums;

namespace ReservationService.DTOs;

public class ActiveReservationDto
{
    public Guid ReservationId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public ReservationStatus Status { get; set; }
    public DateTime ReservedAt { get; set; }

    /// <summary>Populated for Reserved status: days until pickup deadline.</summary>
    public int? DaysUntilExpiry { get; set; }

    /// <summary>Populated for CheckedOut status: days until due date.</summary>
    public int? DaysUntilDue { get; set; }
}