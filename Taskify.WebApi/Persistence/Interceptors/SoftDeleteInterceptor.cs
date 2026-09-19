using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Persistence.Interceptors;

public class SoftDeleteInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplySoftDelete(DbContext? dbContext)
    {
        if (dbContext is null) return;

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in dbContext.ChangeTracker.Entries<ISoftDeletable>()
                     .Where(x => x.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Property(x => x.IsDeleted).CurrentValue = true;
            entry.Property(x => x.DeletedAtUtc).CurrentValue = utcNow;
        }
    }
}