using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Enums;
using ReservationService.Services;

namespace ReservationService.Jobs;

public class WaitlistExpiryJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaitlistExpiryJob> _logger;
    private readonly TimeSpan _interval;

    public WaitlistExpiryJob(
        IServiceScopeFactory scopeFactory,
        ILogger<WaitlistExpiryJob> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var minutes = config.GetValue<int>("WaitlistExpiryJob:IntervalMinutes", 60);
        _interval = TimeSpan.FromMinutes(minutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "WaitlistExpiryJob started. Running every {Interval}.", _interval);

        // Wait one full interval before the first run so startup is not delayed.
        await Task.Delay(_interval, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WaitlistExpiryJob encountered an unhandled error.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReservationServiceContext>();
        var cascadeService = scope.ServiceProvider.GetRequiredService<IWaitlistCascadeService>();

        var now = DateTime.UtcNow;

        // Find all Notified entries whose 48-hour claim window has passed.
        var expired = await db.Waitlists
            .Where(w =>
                w.Status == WaitlistStatus.Notified &&
                w.ClaimDeadline.HasValue &&
                w.ClaimDeadline.Value <= now)
            .ToListAsync(stoppingToken);

        if (expired.Count == 0)
        {
            _logger.LogInformation("WaitlistExpiryJob: no expired entries found.");
            return;
        }

        _logger.LogInformation(
            "WaitlistExpiryJob: processing {Count} expired Notified entries.", expired.Count);

        foreach (var entry in expired)
        {
            // Mark this entry Expired.
            entry.Status = WaitlistStatus.Expired;
            await db.SaveChangesAsync(stoppingToken);

            _logger.LogInformation(
                "WaitlistExpiryJob: entry {WaitlistId} for user {UserId} book {BookId} expired.",
                entry.WaitlistId, entry.UserId, entry.BookId);

            // Cascade: offer the copy to the next eligible patron, or release it.
            await cascadeService.CascadeAsync(entry.BookId, entry.BookTitle, entry.BookAuthor);
        }
    }
}
