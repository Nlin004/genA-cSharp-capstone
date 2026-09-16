namespace CatalogService.DTOs;

public class BookSummaryDto
{
    public Guid BookId { get; set; }
    public string Isbn { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int? PublicationYear { get; set; }
    public string? Description { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }

    /// <summary>"AVAILABLE" when availableCopies > 0, otherwise "CHECKED_OUT".</summary>
    public string Status { get; set; } = string.Empty;
}