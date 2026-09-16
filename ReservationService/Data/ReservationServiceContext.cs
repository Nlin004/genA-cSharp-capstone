using Microsoft.EntityFrameworkCore;
using ReservationService.Models;

namespace ReservationService.Data;

public class ReservationServiceContext : DbContext
{
    public ReservationServiceContext(DbContextOptions<ReservationServiceContext> options)
        : base(options)
    {
    }

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Waitlist> Waitlists => Set<Waitlist>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var reservation = modelBuilder.Entity<Reservation>();
        reservation.HasKey(r => r.ReservationId);

        reservation.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        reservation.Property(r => r.Condition)
            .HasConversion<string>()
            .HasMaxLength(20);

        reservation.Property(r => r.LateFee)
            .HasPrecision(10, 2);

        // Query helpers for Milestone 4 (active reservations, history, limit checks).
        reservation.HasIndex(r => r.UserId);
        reservation.HasIndex(r => r.BookId);
        reservation.HasIndex(r => r.Status);

        var waitlist = modelBuilder.Entity<Waitlist>();
        waitlist.HasKey(w => w.WaitlistId);

        waitlist.Property(w => w.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Query helpers for the waitlist queue / expiry job.
        waitlist.HasIndex(w => w.UserId);
        waitlist.HasIndex(w => w.BookId);
        waitlist.HasIndex(w => w.Status);
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
