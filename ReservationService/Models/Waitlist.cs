using System.ComponentModel.DataAnnotations;
using ReservationService.Enums;

namespace ReservationService.Models;

public class Waitlist : AuditableEntity
{
    [Key]
    public Guid WaitlistId { get; set; }

    // Cross-service reference — Book lives in Catalog Service DB. No FK.
    [Required]
    public Guid BookId { get; set; }

    // Cross-service reference — User lives in User Service DB. No FK.
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public WaitlistStatus Status { get; set; } = WaitlistStatus.Waiting;

    [Required]
    public DateTime JoinedAt { get; set; }

    // Set when a held copy is offered to this entry.
    public DateTime? NotifiedAt { get; set; }

    // NotifiedAt + 48 hours; set alongside NotifiedAt.
    public DateTime? ClaimDeadline { get; set; }

    // Set to the auto-created Reservation's Id once claimed.
    public Guid? ResultingReservationId { get; set; }

    // Cached from Catalog Service at join time.
    [MaxLength(255)]
    public string BookTitle { get; set; } = string.Empty;

    [MaxLength(255)]
    public string BookAuthor { get; set; } = string.Empty;
}