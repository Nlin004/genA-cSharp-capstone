using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data;

public class UserServiceContext : DbContext
{
    public UserServiceContext(DbContextOptions<UserServiceContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var user = modelBuilder.Entity<User>();

        user.HasKey(u => u.UserId);

        user.HasIndex(u => u.Email)
            .IsUnique();

        // Store enums as their string names rather than ints:
        // readable in the DB and stable if enum ordering changes.
        user.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        user.Property(u => u.MembershipStatus)
            .HasConversion<string>()
            .HasMaxLength(20);
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