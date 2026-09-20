using Microsoft.EntityFrameworkCore;
using Shouldly;
using Taskify.WebApi.Domain;
using Taskify.WebApi.Persistence;

namespace Taskify.WebApi.Tests.BackgroundServices;

[Collection(nameof(TestWebApplicationCollection))]
public class SoftDeleteBackgroundServiceTests(TestWebApplicationFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "Should purge eligible soft-deleted items while retaining future and active items")]
    public async Task ExecuteAsync_WithMixedItemStates_ShouldPurgeExpiredSoftDeletedItemsOnly()
    {
        // Arrange
        var utcNow = factory.TimeProvider.GetUtcNow().UtcDateTime;

        var eligibleForPurgeItem = new Item
        {
            Name = "Eligible_SoftDeleted_Item",
            Description = null,
            Priority = Priority.Low,
            DueDateOnUtc = null,
            IsDeleted = true,
            DeletedAtUtc = utcNow.AddMinutes(-30)
        };

        var futureSoftDeletedItem = new Item
        {
            Name = "Future_SoftDeleted_Item",
            Description = null,
            Priority = Priority.Low,
            DueDateOnUtc = null,
            IsDeleted = true,
            DeletedAtUtc = utcNow.AddHours(1)
        };

        var activeItem = new Item
        {
            Name = "Active_NonDeleted_Item",
            Description = null,
            Priority = Priority.Low,
            DueDateOnUtc = null,
            IsDeleted = false,
            DeletedAtUtc = null
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.AddRange([eligibleForPurgeItem, futureSoftDeletedItem, activeItem]);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        });

        // Act
        factory.TimeProvider.Advance(TimeSpan.FromMinutes(5));

        // Assert
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var purgedItemResult = await dbContext.Items
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .FirstOrDefaultAsync(x => x.Id == eligibleForPurgeItem.Id, CancellationToken.None);

            var retainedSoftDeletedItemResult = await dbContext.Items
                .IgnoreQueryFilters([QueryFilters.SoftDelete])
                .FirstOrDefaultAsync(x => x.Id == futureSoftDeletedItem.Id, CancellationToken.None);

            var activeItemResult = await dbContext.Items
                .FirstOrDefaultAsync(x => x.Id == activeItem.Id, CancellationToken.None);

            purgedItemResult.ShouldBeNull();
            retainedSoftDeletedItemResult.ShouldNotBeNull();
            activeItemResult.ShouldNotBeNull();
        });
    }
}