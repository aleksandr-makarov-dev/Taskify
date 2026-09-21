namespace Taskify.WebApi.Infrastructure.Options;

public sealed class ExternalProvidersOptions
{
    public const string SectionName = nameof(ExternalProvidersOptions);
    
    public GoogleOptions Google { get; init; }
}