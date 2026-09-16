using ReservationService.Enums;

namespace ReservationService.DTOs;

public class WaitlistEntryDto
{
    public Guid WaitlistId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public WaitlistStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }

    /// <summary>Populated for Waiting entries: one-based position in the queue.</summary>
    public int? Position { get; set; }

    /// <summary>Populated for Notified entries: deadline to claim the reservation.</summary>
    public DateTime? ClaimDeadline { get; set; }
}