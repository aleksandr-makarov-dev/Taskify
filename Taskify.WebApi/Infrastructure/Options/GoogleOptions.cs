namespace Taskify.WebApi.Infrastructure.Options;

public sealed class GoogleOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}