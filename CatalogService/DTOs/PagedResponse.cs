namespace CatalogService.DTOs;

public class PagedResponse<T>
{
    public List<T> Content { get; set; } = new();

    /// <summary>Zero-based page index that was requested.</summary>
    public int Page { get; set; }

    /// <summary>Maximum number of items per page.</summary>
    public int Size { get; set; }

    /// <summary>Total number of items across all pages.</summary>
    public long TotalElements { get; set; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; set; }

    /// <summary>True when this is the final page.</summary>
    public bool Last { get; set; }
}