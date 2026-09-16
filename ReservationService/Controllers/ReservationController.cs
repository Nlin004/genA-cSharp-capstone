using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.DTOs;
using ReservationService.Enums;
using ReservationService.Models;
using ReservationService.Services;

namespace ReservationService.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private const int MaxActiveReservations = 5;
    private const int ReservationExpiryDays = 7;
    private const int CheckoutDays = 14;
    private const decimal LateFeePerDay = 1.00m;

    private readonly ReservationServiceContext _db;
    private readonly IUserServiceClient _userClient;
    private readonly ICatalogServiceClient _catalogClient;
    private readonly IWaitlistCascadeService _cascadeService;
    private readonly ILogger<ReservationsController> _logger;

    public ReservationsController(
        ReservationServiceContext db,
        IUserServiceClient userClient,
        ICatalogServiceClient catalogClient,
        IWaitlistCascadeService cascadeService,
        ILogger<ReservationsController> logger)
    {
        _db = db;
        _userClient = userClient;
        _catalogClient = catalogClient;
        _cascadeService = cascadeService;
        _logger = logger;
    }

    // ── Helper: extract userId claim ──────────────────────────────────────────

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue("userId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private bool IsLibrarian() =>
        User.FindFirstValue(ClaimTypes.Role)
            ?.Equals("Librarian", StringComparison.OrdinalIgnoreCase) == true;

    // ── POST /api/reservations ────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> CreateReservation([FromBody] CreateReservationRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Invalid token claims."
            });

        // 1. Validate user via UserService
        UserValidateResponse? userInfo;
        try
        {
            userInfo = await _userClient.ValidateUserAsync(userId.Value);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "USER_SUSPENDED",
                Message = ex.Message
            });
        }

        if (userInfo is null)
            return BadRequest(new ErrorResponse
            {
                Error = "USER_NOT_FOUND",
                Message = "Could not validate user."
            });

        // 2. Check active reservation limit (count locally — authoritative source)
        var activeCount = await _db.Reservations
            .CountAsync(r =>
                r.UserId == userId.Value &&
                (r.Status == ReservationStatus.Reserved ||
                 r.Status == ReservationStatus.CheckedOut));

        if (activeCount >= MaxActiveReservations)
            return BadRequest(new ErrorResponse
            {
                Error = "RESERVATION_LIMIT_EXCEEDED",
                Message = $"You already have {activeCount} active reservations. " +
                          $"The maximum is {MaxActiveReservations}."
            });

        // 3. Validate book has available copies via CatalogService availability endpoint.
        //    We use the PATCH endpoint as the source of truth for copies, but we need to
        //    READ availability first — call GET on CatalogService for that.
        //    Rather than adding a second HTTP client method just to read, we attempt the
        //    decrement and treat AVAILABILITY_UNDERFLOW (400) as BOOK_UNAVAILABLE.
        //    First: optimistically attempt the catalog decrement.
        var decremented = await _catalogClient.UpdateAvailabilityAsync(request.BookId, delta: -1);
        if (!decremented)
            return BadRequest(new ErrorResponse
            {
                Error = "BOOK_UNAVAILABLE",
                Message = "This book has no available copies. Consider joining the waitlist."
            });

        // 4. Create the reservation
        var now = DateTime.UtcNow;
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            BookId = request.BookId,
            UserId = userId.Value,
            // BookTitle/Author cached from userInfo isn't available — we cache what CatalogService
            // returns. Since we don't do a separate GET, use empty strings as placeholder;
            // they will be populated from the waitlist cache or left empty until M5 seed data.
            BookTitle = string.Empty,
            BookAuthor = string.Empty,
            Status = ReservationStatus.Reserved,
            ReservedAt = now,
            ExpiresAt = now.AddDays(ReservationExpiryDays),
            RenewalCount = 0
        };

        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Reservation {ReservationId} created for user {UserId} book {BookId}",
            reservation.ReservationId, userId.Value, request.BookId);

        return StatusCode(StatusCodes.Status201Created, new CreateReservationResponse
        {
            ReservationId = reservation.ReservationId,
            BookId = reservation.BookId,
            UserId = reservation.UserId,
            BookTitle = reservation.BookTitle,
            BookAuthor = reservation.BookAuthor,
            Status = reservation.Status,
            ReservedAt = reservation.ReservedAt,
            ExpiresAt = reservation.ExpiresAt,
            Message = "Reservation created successfully."
        });
    }

    // ── GET /api/reservations ─────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetActiveReservations()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Invalid token claims."
            });

        var active = await _db.Reservations
            .Where(r =>
                r.UserId == userId.Value &&
                (r.Status == ReservationStatus.Reserved ||
                 r.Status == ReservationStatus.CheckedOut))
            .OrderBy(r => r.ReservedAt)
            .ToListAsync();

        var now = DateTime.UtcNow;

        var dtos = active.Select(r => new ActiveReservationDto
        {
            ReservationId = r.ReservationId,
            BookId = r.BookId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            Status = r.Status,
            ReservedAt = r.ReservedAt,
            DaysUntilExpiry = r.Status == ReservationStatus.Reserved && r.ExpiresAt.HasValue
                ? Math.Max(0, (int)Math.Ceiling((r.ExpiresAt.Value - now).TotalDays))
                : null,
            DaysUntilDue = r.Status == ReservationStatus.CheckedOut && r.DueDate.HasValue
                ? Math.Max(0, (int)Math.Ceiling((r.DueDate.Value - now).TotalDays))
                : null
        }).ToList();

        return Ok(new ActiveReservationsResponse
        {
            Reservations = dtos,
            TotalActive = dtos.Count
        });
    }

    // ── POST /api/reservations/{reservationId}/checkout ───────────────────────

    [HttpPost("{reservationId:guid}/checkout")]
    public async Task<IActionResult> Checkout(Guid reservationId, [FromBody] CheckoutRequest request)
    {
        if (!IsLibrarian())
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
            {
                Error = "FORBIDDEN",
                Message = "Only Librarians can process checkouts."
            });

        var reservation = await _db.Reservations.FindAsync(reservationId);
        if (reservation is null)
            return NotFound(new ErrorResponse
            {
                Error = "RESERVATION_NOT_FOUND",
                Message = $"No reservation found with ID {reservationId}."
            });

        if (reservation.Status != ReservationStatus.Reserved)
            return BadRequest(new ErrorResponse
            {
                Error = "INVALID_STATUS",
                Message = $"Cannot checkout a reservation with status {reservation.Status}. " +
                          "Only Reserved reservations can be checked out."
            });

        var now = DateTime.UtcNow;
        reservation.Status = ReservationStatus.CheckedOut;
        reservation.CheckedOutAt = now;
        reservation.DueDate = now.AddDays(CheckoutDays);

        if (!string.IsNullOrWhiteSpace(request.Notes))
            reservation.Notes = request.Notes;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Reservation {ReservationId} checked out. Due: {DueDate}",
            reservationId, reservation.DueDate);

        return Ok(new CheckoutResponse
        {
            ReservationId = reservation.ReservationId,
            Status = reservation.Status,
            CheckedOutAt = reservation.CheckedOutAt!.Value,
            DueDate = reservation.DueDate!.Value,
            Message = $"Book checked out successfully. Due date: {reservation.DueDate.Value:yyyy-MM-dd}."
        });
    }

    // ── POST /api/reservations/{reservationId}/return ─────────────────────────

    [HttpPost("{reservationId:guid}/return")]
    public async Task<IActionResult> Return(Guid reservationId, [FromBody] ReturnRequest request)
    {
        if (!IsLibrarian())
            return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
            {
                Error = "FORBIDDEN",
                Message = "Only Librarians can process returns."
            });

        var reservation = await _db.Reservations.FindAsync(reservationId);
        if (reservation is null)
            return NotFound(new ErrorResponse
            {
                Error = "RESERVATION_NOT_FOUND",
                Message = $"No reservation found with ID {reservationId}."
            });

        if (reservation.Status != ReservationStatus.CheckedOut)
            return BadRequest(new ErrorResponse
            {
                Error = "INVALID_STATUS",
                Message = $"Cannot return a reservation with status {reservation.Status}. " +
                          "Only CheckedOut reservations can be returned."
            });

        var now = DateTime.UtcNow;

        // Late fee calculation
        int lateDays = 0;
        decimal lateFee = 0m;
        if (reservation.DueDate.HasValue && now > reservation.DueDate.Value)
        {
            lateDays = (int)Math.Ceiling((now - reservation.DueDate.Value).TotalDays);
            lateFee = lateDays * LateFeePerDay;
        }

        reservation.Status = ReservationStatus.Returned;
        reservation.ReturnedAt = now;
        reservation.Condition = request.Condition;
        reservation.LateDays = lateDays;
        reservation.LateFee = lateFee;

        if (!string.IsNullOrWhiteSpace(request.Notes))
            reservation.Notes = request.Notes;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Reservation {ReservationId} returned. LateDays={LateDays}, LateFee={LateFee}",
            reservationId, lateDays, lateFee);

        // Waitlist cascade — this handles both the "give copy to next patron" and
        // "increment availableCopies if no one is waiting" cases.
        await _cascadeService.CascadeAsync(
            reservation.BookId,
            reservation.BookTitle,
            reservation.BookAuthor);

        var message = lateDays > 0
            ? $"Book returned {lateDays} day(s) late. Late fee: ${lateFee:F2}."
            : "Book returned on time. Thank you!";

        return Ok(new ReturnResponse
        {
            ReservationId = reservation.ReservationId,
            ReturnedAt = reservation.ReturnedAt!.Value,
            LateDays = lateDays,
            LateFee = lateFee,
            Message = message
        });
    }

    // ── GET /api/reservations/history ─────────────────────────────────────────

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Invalid token claims."
            });

        if (page < 0) page = 0;
        if (size < 1) size = 1;
        if (size > 100) size = 100;

        var query = _db.Reservations
            .Where(r => r.UserId == userId.Value)
            .OrderByDescending(r => r.ReservedAt);

        var totalElements = await query.CountAsync();
        var totalPages = totalElements == 0
            ? 0
            : (int)Math.Ceiling((double)totalElements / size);

        var items = await query
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var content = items.Select(r => new HistoryItemDto
        {
            ReservationId = r.ReservationId,
            BookId = r.BookId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            Status = r.Status,
            ReservedAt = r.ReservedAt,
            CheckedOutAt = r.CheckedOutAt,
            DueDate = r.DueDate,
            ReturnedAt = r.ReturnedAt,
            LateDays = r.LateDays,
            LateFee = r.LateFee,
            WasLate = r.LateDays.HasValue && r.LateDays.Value > 0
        }).ToList();

        return Ok(new PagedResponse<HistoryItemDto>
        {
            Content = content,
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            Last = (page + 1) >= totalPages || totalElements == 0
        });
    }

    // ── GET /api/reservations/statistics/{userId} ─────────────────────────────
    // Internal — called by UserService to populate profile statistics.

    [HttpGet("statistics/{userId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatistics(Guid userId)
    {
        var activeCount = await _db.Reservations
            .CountAsync(r =>
                r.UserId == userId &&
                (r.Status == ReservationStatus.Reserved ||
                 r.Status == ReservationStatus.CheckedOut));

        var totalCount = await _db.Reservations
            .CountAsync(r => r.UserId == userId);

        return Ok(new ReservationStatisticsDto
        {
            UserId = userId,
            ActiveReservations = activeCount,
            BorrowingHistory = totalCount
        });
    }

    // ── POST /api/reservations/waitlist ───────────────────────────────────────

    [HttpPost("waitlist")]
    public async Task<IActionResult> JoinWaitlist([FromBody] JoinWaitlistRequest request)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Invalid token claims."
            });

        // Check for an existing active (Waiting) entry for this user+book combination.
        var alreadyWaiting = await _db.Waitlists
            .AnyAsync(w =>
                w.UserId == userId.Value &&
                w.BookId == request.BookId &&
                w.Status == WaitlistStatus.Waiting);

        if (alreadyWaiting)
            return BadRequest(new ErrorResponse
            {
                Error = "ALREADY_WAITLISTED",
                Message = "You are already on the waitlist for this book."
            });

        // Patron may only join when availableCopies = 0. We enforce this by attempting
        // a -1 delta on CatalogService and treating success as "book IS available".
        // Instead, we need to READ availability. We call UpdateAvailabilityAsync with
        // delta=0 which CatalogService rejects (INVALID_DELTA), confirming the endpoint
        // exists but not consuming a copy. To actually read, we need a GET.
        // Since ICatalogServiceClient only exposes UpdateAvailabilityAsync, we check
        // indirectly: try delta=-1. If it succeeds the book is available — reverse it and
        // return BOOK_AVAILABLE. If it returns false (400/underflow) the book is unavailable.
        var available = await _catalogClient.UpdateAvailabilityAsync(request.BookId, delta: -1);
        if (available)
        {
            // Book was available — reverse the decrement and tell the patron to reserve directly.
            await _catalogClient.UpdateAvailabilityAsync(request.BookId, delta: +1);
            return BadRequest(new ErrorResponse
            {
                Error = "BOOK_AVAILABLE",
                Message = "This book has available copies. Please reserve it directly instead of joining the waitlist."
            });
        }

        // Book is unavailable — add to waitlist.
        var now = DateTime.UtcNow;
        var entry = new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = request.BookId,
            UserId = userId.Value,
            BookTitle = string.Empty,
            BookAuthor = string.Empty,
            Status = WaitlistStatus.Waiting,
            JoinedAt = now
        };

        _db.Waitlists.Add(entry);
        await _db.SaveChangesAsync();

        // Compute one-based queue position (count of Waiting entries for this book including this one).
        var position = await _db.Waitlists
            .CountAsync(w =>
                w.BookId == request.BookId &&
                w.Status == WaitlistStatus.Waiting &&
                w.JoinedAt <= entry.JoinedAt);

        _logger.LogInformation(
            "User {UserId} joined waitlist for book {BookId} at position {Position}",
            userId.Value, request.BookId, position);

        return StatusCode(StatusCodes.Status201Created, new JoinWaitlistResponse
        {
            WaitlistId = entry.WaitlistId,
            BookId = entry.BookId,
            BookTitle = entry.BookTitle,
            BookAuthor = entry.BookAuthor,
            Status = entry.Status,
            JoinedAt = entry.JoinedAt,
            Position = position
        });
    }

    // ── GET /api/reservations/waitlist ────────────────────────────────────────

    [HttpGet("waitlist")]
    public async Task<IActionResult> GetMyWaitlist()
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Invalid token claims."
            });

        var entries = await _db.Waitlists
            .Where(w =>
                w.UserId == userId.Value &&
                (w.Status == WaitlistStatus.Waiting ||
                 w.Status == WaitlistStatus.Notified))
            .OrderBy(w => w.JoinedAt)
            .ToListAsync();

        // For each Waiting entry, compute position by counting earlier Waiting entries for the same book.
        var dtos = new List<WaitlistEntryDto>();
        foreach (var entry in entries)
        {
            int? position = null;
            if (entry.Status == WaitlistStatus.Waiting)
            {
                position = await _db.Waitlists
                    .CountAsync(w =>
                        w.BookId == entry.BookId &&
                        w.Status == WaitlistStatus.Waiting &&
                        w.JoinedAt <= entry.JoinedAt);
            }

            dtos.Add(new WaitlistEntryDto
            {
                WaitlistId = entry.WaitlistId,
                BookId = entry.BookId,
                BookTitle = entry.BookTitle,
                BookAuthor = entry.BookAuthor,
                Status = entry.Status,
                JoinedAt = entry.JoinedAt,
                Position = position,
                ClaimDeadline = entry.ClaimDeadline
            });
        }

        return Ok(dtos);
    }

    // ── DELETE /api/reservations/waitlist/{waitlistId} ────────────────────────

    [HttpDelete("waitlist/{waitlistId:guid}")]
    public async Task<IActionResult> LeaveWaitlist(Guid waitlistId)
    {
        var userId = GetUserId();
        if (userId is null)
            return Unauthorized(new ErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Invalid token claims."
            });

        var entry = await _db.Waitlists.FindAsync(waitlistId);

        // 404 if not found OR if it belongs to a different user (don't reveal existence).
        if (entry is null || entry.UserId != userId.Value)
            return NotFound(new ErrorResponse
            {
                Error = "WAITLIST_ENTRY_NOT_FOUND",
                Message = "Waitlist entry not found."
            });

        if (entry.Status != WaitlistStatus.Waiting && entry.Status != WaitlistStatus.Notified)
            return BadRequest(new ErrorResponse
            {
                Error = "INVALID_STATUS",
                Message = "Only Waiting or Notified entries can be cancelled."
            });

        var wasNotified = entry.Status == WaitlistStatus.Notified;
        entry.Status = WaitlistStatus.Cancelled;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} left waitlist entry {WaitlistId} (wasNotified={WasNotified})",
            userId.Value, waitlistId, wasNotified);

        // If the entry was Notified (holding a live claim), immediately cascade
        // the copy to the next eligible patron — same logic as natural expiry.
        if (wasNotified)
        {
            await _cascadeService.CascadeAsync(
                entry.BookId,
                entry.BookTitle,
                entry.BookAuthor);
        }

        return Ok(new { message = "You have been removed from the waitlist." });
    }
}
