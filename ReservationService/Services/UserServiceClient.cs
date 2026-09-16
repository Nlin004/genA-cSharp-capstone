using System.Net;
using System.Text.Json;
using ReservationService.DTOs;

namespace ReservationService.Services;

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserValidateResponse?> ValidateUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}/validate");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("UserService: user {UserId} not found", userId);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                // UserService returns 400 when membership is Suspended
                throw new InvalidOperationException("User membership is suspended.");
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserValidateResponse>(content, _jsonOptions);
        }
        catch (InvalidOperationException)
        {
            throw; // Let suspended-user exception propagate
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "UserService unavailable while validating user {UserId}", userId);
            return null;
        }
    }
}