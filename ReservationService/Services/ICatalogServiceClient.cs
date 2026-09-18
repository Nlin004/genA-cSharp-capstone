using ReservationService.DTOs;

namespace ReservationService.Services;

public interface ICatalogServiceClient
{
    Task<bool> UpdateAvailabilityAsync(Guid bookId, int delta);
    // PATCH -> delta is -1 when a book is reserved, or +1 when the book is returned or released.
    Task<CatalogBookDto?> GetBookAsync(Guid bookId);
    // GET -> title/author for caching onto Reservation/Waitlist rows. Returns null if
    // CatalogService is unreachable or the book no longer exists.
}