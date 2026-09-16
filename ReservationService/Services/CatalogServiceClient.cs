using System.Text;
using System.Text.Json;
using ReservationService.DTOs;

namespace ReservationService.Services;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        try
        {
            var body = JsonSerializer.Serialize(
                new BookAvailabilityUpdateRequest { Delta = delta },
                _jsonOptions);

            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _httpClient
                .PatchAsync($"/api/catalog/books/{bookId}/availability", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "CatalogService returned {StatusCode} updating availability for book {BookId} delta={Delta}",
                    (int)response.StatusCode, bookId, delta);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "CatalogService unavailable while updating availability for book {BookId}", bookId);
            return false;
        }
    }
}