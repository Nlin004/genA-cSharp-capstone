using ReservationService.DTOs;

namespace ReservationService.Services;

public interface IUserServiceClient
{
    Task<UserValidateResponse?> ValidateUserAsync(Guid userId);
}