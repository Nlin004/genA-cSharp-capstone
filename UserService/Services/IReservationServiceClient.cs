using UserService.DTOs;

namespace UserService.Services;

public interface IReservationServiceClient
{
    Task<ReservationStatisticsDto?> GetStatisticsAsync(Guid userId);
}