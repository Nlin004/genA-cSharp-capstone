using System.ComponentModel.DataAnnotations;
using ReservationService.Enums;

namespace ReservationService.Models;

public class Reservation : AuditableEntity
{
    [Key]
    public Guid ReservationId { get; set; }

    // Cross-service reference — Book lives in Catalog Service DB. No FK.
    [Required]
    public Guid BookId { get; set; }

    // Cross-service reference — User lives in User Service DB. No FK.
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;

    [Required]
    public DateTime ReservedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime? CheckedOutAt { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? ReturnedAt { get; set; }

    public int RenewalCount { get; set; }

    public int? LateDays { get; set; }

    public decimal? LateFee { get; set; }

    public BookCondition? Condition { get; set; }

    public string? Notes { get; set; }

    // Cached from Catalog Service at reservation time.
    [MaxLength(255)]
    public string BookTitle { get; set; } = string.Empty;

    [MaxLength(255)]
    public string BookAuthor { get; set; } = string.Empty;
}