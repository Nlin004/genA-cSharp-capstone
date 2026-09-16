using System.ComponentModel.DataAnnotations;
using UserService.Enums;

namespace UserService.Models;

public class User : AuditableEntity
{
    [Key]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    public Role Role { get; set; } = Role.Patron;

    [Required]
    public MembershipStatus MembershipStatus { get; set; } = MembershipStatus.Active;

    public DateTime? MemberSince { get; set; }
}