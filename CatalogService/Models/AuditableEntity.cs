namespace CatalogService.Models;

public abstract class AuditableEntity
{
    // base type for entities that carry audit timstamps.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}