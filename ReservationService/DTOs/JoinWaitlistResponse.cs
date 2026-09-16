using ReservationService.Enums;

namespace ReservationService.DTOs;

public class JoinWaitlistResponse
{
    public Guid WaitlistId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public WaitlistStatus Status { get; set; }
    public DateTime JoinedAt { get; set; }

    /// One-based queue position among all Waiting entries for this book
    public int Position { get; set; }
}