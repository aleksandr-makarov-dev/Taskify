namespace Taskify.WebApi.Infrastructure.Options;

public sealed class ProviderOptions
{
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
}