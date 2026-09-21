namespace Taskify.WebApi.Infrastructure.Options;

public sealed class ExternalProvidersOptions
{
    public const string SectionName = nameof(ExternalProvidersOptions);

    public required ProviderOptions Google { get; init; }
    public required ProviderOptions Github { get; init; }
}