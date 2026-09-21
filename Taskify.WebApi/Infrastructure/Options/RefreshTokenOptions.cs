namespace Taskify.WebApi.Infrastructure.Options;

public class RefreshTokenOptions
{
    public const string SectionName = nameof(RefreshTokenOptions);
    
    public TimeSpan Expiration { get; init; }
}