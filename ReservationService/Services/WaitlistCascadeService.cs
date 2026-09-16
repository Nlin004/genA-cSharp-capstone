using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Enums;
using ReservationService.Models;

namespace ReservationService.Services;

public class WaitlistCascadeService : IWaitlistCascadeService
{
    private const int MaxActiveReservations = 5;
    private const int ClaimWindowHours = 48;
    private const int CheckoutDays = 14;

    private readonly ReservationServiceContext _db;
    private readonly ICatalogServiceClient _catalogClient;
    private readonly ILogger<WaitlistCascadeService> _logger;

    public WaitlistCascadeService(
        ReservationServiceContext db,
        ICatalogServiceClient catalogClient,
        ILogger<WaitlistCascadeService> logger)
    {
        _db = db;
        _catalogClient = catalogClient;
        _logger = logger;
    }

    public async Task CascadeAsync(Guid bookId, string bookTitle, string bookAuthor)
    {
        // Fetch all Waiting entries for this book, oldest first (queue order).
        var queue = await _db.Waitlists
            .Where(w => w.BookId == bookId && w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.JoinedAt)
            .ToListAsync();

        foreach (var entry in queue)
        {
            // Count this patron's currently active reservations.
            var activeCount = await _db.Reservations
                .CountAsync(r =>
                    r.UserId == entry.UserId &&
                    (r.Status == ReservationStatus.Reserved ||
                     r.Status == ReservationStatus.CheckedOut));

            if (activeCount >= MaxActiveReservations)
            {
                // Patron is over the limit — expire their entry and try the next one.
                entry.Status = WaitlistStatus.Expired;
                _logger.LogInformation(
                    "Waitlist entry {WaitlistId} for user {UserId} expired: reservation limit reached",
                    entry.WaitlistId, entry.UserId);
                continue;
            }

            // Found an eligible patron — auto-create a Reserved reservation for them.
            var now = DateTime.UtcNow;
            var reservation = new Reservation
            {
                ReservationId = Guid.NewGuid(),
                BookId = bookId,
                UserId = entry.UserId,
                BookTitle = bookTitle,
                BookAuthor = bookAuthor,
                Status = ReservationStatus.Reserved,
                ReservedAt = now,
                ExpiresAt = now.AddDays(7),
                RenewalCount = 0
            };

            _db.Reservations.Add(reservation);

            // Mark the waitlist entry Notified with a 48-hour claim deadline.
            entry.Status = WaitlistStatus.Notified;
            entry.NotifiedAt = now;
            entry.ClaimDeadline = now.AddHours(ClaimWindowHours);
            entry.ResultingReservationId = reservation.ReservationId;

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Waitlist entry {WaitlistId} notified: reservation {ReservationId} auto-created for user {UserId}",
                entry.WaitlistId, reservation.ReservationId, entry.UserId);

            // Copy is consumed — do NOT increment availableCopies.
            return;
        }

        // Queue exhausted (or was empty) — release the copy back to general availability.
        await _db.SaveChangesAsync(); // Persist any Expired status changes first.

        var updated = await _catalogClient.UpdateAvailabilityAsync(bookId, delta: +1);
        if (updated)
            _logger.LogInformation(
                "Book {BookId} availableCopies incremented: no eligible waitlist patron found", bookId);
        else
            _logger.LogWarning(
                "Book {BookId}: CatalogService unavailable when attempting to increment availableCopies", bookId);
    }
}
