namespace CatalogService.DTOs;

public class BookAvailabilityUpdateRequest
{
    public int Delta { get; set; }
    // increment the number of available copies! or decrease by 1 if book is reserved.
}