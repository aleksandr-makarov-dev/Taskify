using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taskify.WebApi.Infrastructure.Options;
using Taskify.WebApi.Persistence;

namespace Taskify.WebApi.Infrastructure.BackgroundServices;

public sealed class SoftDeleteBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<SoftDeleteBackgroundService> logger,
    TimeProvider timeProvider,
    IOptions<SoftDeleteOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Soft-delete cleanup background service started. Check interval: {CheckInterval}, retention period: {RetentionPeriod}.",
            options.Value.CheckInterval,
            options.Value.RetentionPeriod);

        using var timer = new PeriodicTimer(options.Value.CheckInterval, timeProvider);

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
        var expirationTime = utcNow - options.Value.RetentionPeriod;

        var count = await dbContext.Items
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(x => x.IsDeleted && x.DeletedAtUtc.HasValue && x.DeletedAtUtc.Value <= expirationTime)
            .OrderBy(x => x.DeletedAtUtc)
            .Take(options.Value.BatchSize)
            .ExecuteDeleteAsync(cancellationToken);

        if (count > 0)
        {
            logger.LogInformation("Cleaned up {Count} soft-deleted items at {UtcNow}.", count, utcNow);
        }
    }
}