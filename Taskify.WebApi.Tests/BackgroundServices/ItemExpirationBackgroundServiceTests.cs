using Microsoft.EntityFrameworkCore;
using Shouldly;
using Taskify.WebApi.Domain.Items;

namespace Taskify.WebApi.Tests.BackgroundServices;

[Collection(nameof(TestWebApplicationCollection))]
public class ItemExpirationBackgroundServiceTests(TestWebApplicationFactory factory) : IAsyncLifetime
{
    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact(DisplayName = "Marks overdue items as expired while leaving future items active")]
    public async Task ExecuteAsync_WhenOverdueItemsExist_ShouldMarkAsExpiredOnly()
    {
        // Arrange
        var utcNow = factory.TimeProvider.GetUtcNow().UtcDateTime;

        var overdueItem = new Item
        {
            Name = "Overdue_Item",
            Description = null,
            Priority = Priority.Low,
            DueDateOnUtc = utcNow.AddMinutes(-15)
        };

        var futureDueDateItem = new Item
        {
            Name = "Future_DueDate_Item",
            Description = null,
            Priority = Priority.Low,
            DueDateOnUtc = utcNow.AddMinutes(15)
        };

        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            dbContext.Items.AddRange([overdueItem, futureDueDateItem]);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        });

        // Act
        factory.TimeProvider.Advance(TimeSpan.FromMinutes(5));

        // Assert
        await factory.ExecuteDbContextAsync(async dbContext =>
        {
            var overdueItemResult = await dbContext.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == overdueItem.Id, CancellationToken.None);

            var futureItemResult = await dbContext.Items
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == futureDueDateItem.Id, CancellationToken.None);

            overdueItemResult.ShouldNotBeNull();
            overdueItemResult.IsExpired.ShouldBeTrue();
            overdueItemResult.ExpiredAtUtc.ShouldNotBeNull();

            futureItemResult.ShouldNotBeNull();
            futureItemResult.IsExpired.ShouldBeFalse();
            futureItemResult.ExpiredAtUtc.ShouldBeNull();
        });
    }
}