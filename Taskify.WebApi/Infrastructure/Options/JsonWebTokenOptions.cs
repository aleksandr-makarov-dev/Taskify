namespace Taskify.WebApi.Infrastructure.Options;

public sealed class JsonWebTokenOptions
{
    public const string SectionName = nameof(JsonWebTokenOptions);

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SecretKey { get; init; }
    public TimeSpan Expiration { get; init; }
}