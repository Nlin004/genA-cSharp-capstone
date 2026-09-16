using UserService.Enums;
using UserService.Models;

namespace UserService.Data;

public static class UserServiceSeeder
{
    public static void Seed(UserServiceContext db)
    {
        if (db.Users.Any(u => u.Role == Role.Librarian))
            return;

        var librarian = new User
        {
            UserId = Guid.NewGuid(),
            Email = "librarian@library.com",
            // Password: Librarian@123
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Librarian@123", 12),
            FirstName = "Head",
            LastName = "Librarian",
            PhoneNumber = "+1-555-000-0001",
            Role = Role.Librarian,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        db.Users.Add(librarian);
        db.SaveChanges();
    }
}