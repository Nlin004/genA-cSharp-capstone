// filename: CatalogService/Data/CatalogServiceContext.cs
using Microsoft.EntityFrameworkCore;
using CatalogService.Models;

namespace CatalogService.Data;

public class CatalogServiceContext : DbContext
{
    public CatalogServiceContext(DbContextOptions<CatalogServiceContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var book = modelBuilder.Entity<Book>();

        book.HasKey(b => b.BookId);

        book.HasIndex(b => b.Isbn)
            .IsUnique();
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }
    }
}