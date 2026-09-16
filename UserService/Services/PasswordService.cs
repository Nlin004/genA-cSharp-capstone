namespace UserService.Services;

public class PasswordService : IPasswordService
{
    // Work factor 12 is a reasonable default: slow enough to resist brute force,
    // fast enough for a single login at ~250ms on typical hardware.
    private const int WorkFactor = 12;

    public string Hash(string plainTextPassword) =>
        BCrypt.Net.BCrypt.HashPassword(plainTextPassword, WorkFactor);

    public bool Verify(string plainTextPassword, string hashedPassword) =>
        BCrypt.Net.BCrypt.Verify(plainTextPassword, hashedPassword);
}