using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Taskify.WebApi.Infrastructure.Options;
using Taskify.WebApi.Persistence;

namespace Taskify.WebApi.Infrastructure.BackgroundServices;

public sealed class ItemExpirationBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ItemExpirationBackgroundService> logger,
    TimeProvider timeProvider,
    IOptions<ItemExpirationOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Expiration checker started. Interval: {Interval}", options.Value.CheckInterval);

        using var timer = new PeriodicTimer(options.Value.CheckInterval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckExpiredItemsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Expiration checker stopped.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Item expiration background service failed unexpectedly.");
            throw;
        }
    }

    private async Task CheckExpiredItemsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var count = await dbContext.Items
            .Where(x =>
                !x.IsComplete &&
                !x.IsExpired &&
                x.DueDateOnUtc.HasValue &&
                x.DueDateOnUtc <= utcNow)
            .OrderBy(x => x.DueDateOnUtc)
            .Take(options.Value.BatchSize)
            .ExecuteUpdateAsync(setters =>
            {
                setters
                    .SetProperty(x => x.IsExpired, true)
                    .SetProperty(x => x.ExpiredAtUtc, utcNow);
            }, cancellationToken);

        if (count > 0)
        {
            logger.LogInformation("Marked {Count} items as expired at {UtcNow}.", count, utcNow);
        }
    }
}