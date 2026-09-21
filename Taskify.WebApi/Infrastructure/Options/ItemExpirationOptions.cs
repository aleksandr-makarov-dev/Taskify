namespace Taskify.WebApi.Infrastructure.Options;

public sealed class ItemExpirationOptions
{
    public const string SectionName = nameof(ItemExpirationOptions);
    
    public TimeSpan CheckInterval { get; init; }
    public int BatchSize { get; init; }
}