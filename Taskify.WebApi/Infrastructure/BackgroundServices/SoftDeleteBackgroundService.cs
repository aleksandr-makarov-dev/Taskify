using Microsoft.EntityFrameworkCore;
using Taskify.WebApi.Persistence;

namespace Taskify.WebApi.Infrastructure.BackgroundServices;

public sealed class SoftDeleteBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SoftDeleteBackgroundService> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 1000;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Soft-delete cleanup background service started. Check interval: {CheckInterval}, retention period: {RetentionPeriod}.",
            CheckInterval,
            RetentionPeriod);

        using var timer = new PeriodicTimer(CheckInterval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanupItemsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Soft-delete cleanup background service stopped.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Soft-delete cleanup background service failed unexpectedly.");

            throw;
        }
    }

    private async Task CleanupItemsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var expirationTime = utcNow - RetentionPeriod;

        var count = await dbContext.Items
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(x => x.IsDeleted && x.DeletedAtUtc.HasValue && x.DeletedAtUtc.Value <= expirationTime)
            .OrderBy(x => x.DeletedAtUtc)
            .Take(BatchSize)
            .ExecuteDeleteAsync(cancellationToken);

        if (count > 0)
        {
            logger.LogInformation("Cleaned up {Count} soft-deleted items at {UtcNow}.", count, utcNow);
        }
    }
}