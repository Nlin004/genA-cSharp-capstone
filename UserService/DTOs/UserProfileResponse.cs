using UserService.Enums;
namespace UserService.DTOs;

public class UserProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Role Role { get; set; }
    public MembershipStatus MembershipStatus { get; set; }
    public DateTime? MemberSince { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}