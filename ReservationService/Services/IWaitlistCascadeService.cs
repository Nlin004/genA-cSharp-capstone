namespace ReservationService.Services;

public interface IWaitlistCascadeService
{
    /// <summary>
    /// Called after a copy becomes available (return or cancelled Notified entry).
    /// Walks the Waiting queue for the given book, oldest-first.
    /// For each entry: if the patron has fewer than 5 active reservations, auto-creates a
    /// Reserved reservation for them, marks their entry Notified with a 48-hour ClaimDeadline,
    /// and stops. If they are over the limit, expires that entry and continues.
    /// If the queue is exhausted, increments availableCopies on CatalogService (+1).
    /// </summary>
    Task CascadeAsync(Guid bookId, string bookTitle, string bookAuthor);
}