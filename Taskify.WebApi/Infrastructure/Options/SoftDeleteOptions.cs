namespace Taskify.WebApi.Infrastructure.Options;

public class SoftDeleteOptions
{
    public const string SectionName = nameof(SoftDeleteOptions);

    public TimeSpan CheckInterval { get; init; }
    public TimeSpan RetentionPeriod { get; init; }
    public int BatchSize { get; init; }
}