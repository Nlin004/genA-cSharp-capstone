using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CatalogService.Data;
using CatalogService.DTOs;
using CatalogService.Models;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/catalog/books")]
public class BooksController : ControllerBase
{
    private readonly CatalogServiceContext _db;
    private readonly ILogger<BooksController> _logger;

    public BooksController(CatalogServiceContext db, ILogger<BooksController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET /api/catalog/books
    [HttpGet]
    public async Task<IActionResult> GetBooks(
        [FromQuery] int page = 0,
        [FromQuery] int size = 20,
        [FromQuery] string sortBy = "title",
        [FromQuery] string sortOrder = "asc",
        [FromQuery] string? query = null,
        [FromQuery] string? genre = null,
        [FromQuery] string? isbn = null,
        [FromQuery] bool availableOnly = false)
    {
        // Clamp to safe values
        if (page < 0) page = 0;
        if (size < 1) size = 1;
        if (size > 100) size = 100;

        IQueryable<Book> books = _db.Books;

        // ── Filtering ────────────────────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.Trim().ToLower();
            books = books.Where(b =>
                b.Title.ToLower().Contains(lower) ||
                b.Author.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(genre))
        {
            var g = genre.Trim().ToLower();
            books = books.Where(b => b.Genre.ToLower() == g);
        }

        if (!string.IsNullOrWhiteSpace(isbn))
        {
            var i = isbn.Trim().ToLower();
            books = books.Where(b => b.Isbn.ToLower() == i);
        }

        if (availableOnly)
            books = books.Where(b => b.AvailableCopies > 0);

        // ── Sorting ──────────────────────────────────────────────────────────
        var descending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        books = sortBy.Trim().ToLower() switch
        {
            "author" => descending
                ? books.OrderByDescending(b => b.Author)
                : books.OrderBy(b => b.Author),
            "publicationyear" => descending
                ? books.OrderByDescending(b => b.PublicationYear)
                : books.OrderBy(b => b.PublicationYear),
            _ => descending                         // default: title
                ? books.OrderByDescending(b => b.Title)
                : books.OrderBy(b => b.Title)
        };

        // ── Pagination ───────────────────────────────────────────────────────
        var totalElements = await books.CountAsync();
        var totalPages = totalElements == 0
            ? 0
            : (int)Math.Ceiling((double)totalElements / size);
        var isLast = (page + 1) >= totalPages || totalElements == 0;

        var items = await books
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        var content = items.Select(ToSummary).ToList();

        return Ok(new PagedResponse<BookSummaryDto>
        {
            Content = content,
            Page = page,
            Size = size,
            TotalElements = totalElements,
            TotalPages = totalPages,
            Last = isLast
        });
    }

    // GET /api/catalog/books/{bookId}
    [HttpGet("{bookId:guid}")]
    public async Task<IActionResult> GetBook(Guid bookId)
    {
        var book = await _db.Books.FindAsync(bookId);

        if (book is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "BOOK_NOT_FOUND",
                Message = $"No book found with ID {bookId}."
            });
        }

        return Ok(ToDetail(book));
    }
    
    // PATCH /api/catalog/books/{bookId}/availability
    // Internal — called by Reservation Service when a book is reserved (+delta -1)
    // or returned / claim released (+delta +1).
    [HttpPatch("{bookId:guid}/availability")]
    public async Task<IActionResult> UpdateAvailability(
        Guid bookId,
        [FromBody] BookAvailabilityUpdateRequest request)
    {
        if (request.Delta != 1 && request.Delta != -1)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "INVALID_DELTA",
                Message = "Delta must be exactly 1 or -1."
            });
        }

        var book = await _db.Books.FindAsync(bookId);
        if (book is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "BOOK_NOT_FOUND",
                Message = $"No book found with ID {bookId}."
            });
        }

        var newCount = book.AvailableCopies + request.Delta;

        if (newCount < 0)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "AVAILABILITY_UNDERFLOW",
                Message = "Available copies cannot go below zero."
            });
        }

        if (newCount > book.TotalCopies)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "AVAILABILITY_OVERFLOW",
                Message = "Available copies cannot exceed total copies."
            });
        }

        book.AvailableCopies = newCount;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Book {BookId} availableCopies updated by {Delta} → now {Count}",
            bookId, request.Delta, book.AvailableCopies);

        return Ok(new { bookId = book.BookId, availableCopies = book.AvailableCopies });
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string CalculateStatus(Book book) =>
        book.AvailableCopies > 0 ? "AVAILABLE" : "CHECKED_OUT";

    private BookSummaryDto ToSummary(Book book) => new()
    {
        BookId = book.BookId,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Genre = book.Genre,
        PublicationYear = book.PublicationYear,
        Description = book.Description,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        Status = CalculateStatus(book)
    };

    private BookDetailDto ToDetail(Book book) => new()
    {
        BookId = book.BookId,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Genre = book.Genre,
        PublicationYear = book.PublicationYear,
        Description = book.Description,
        Publisher = book.Publisher,
        PageCount = book.PageCount,
        Language = book.Language,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        Status = CalculateStatus(book),
        CreatedAt = book.CreatedAt,
        UpdatedAt = book.UpdatedAt
    };
}
