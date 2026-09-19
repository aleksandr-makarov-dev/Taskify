using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Taskify.WebApi.Domain;

namespace Taskify.WebApi.Persistence.Interceptors;

public class AuditInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext? dbContext)
    {
        if (dbContext is null) return;

        var entries = dbContext.ChangeTracker.Entries<IAuditable>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified);

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(x => x.CreatedAtUtc).CurrentValue = utcNow;
                    break;
                case EntityState.Modified:
                    entry.Property(x => x.LastModifiedAtUtc).CurrentValue = utcNow;
                    break;
            }
        }
    }
}