namespace ReservationService.DTOs;

public class PagedResponse<T>
{
    public List<T> Content { get; set; } = new();
    public int Page { get; set; }
    public int Size { get; set; }
    public long TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool Last { get; set; }
}