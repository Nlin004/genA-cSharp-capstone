namespace UserService.Services;

public interface IPasswordService
{
    string Hash(string plainTextPassword);
    bool Verify(string plainTextPassword, string hashedPassword);
}