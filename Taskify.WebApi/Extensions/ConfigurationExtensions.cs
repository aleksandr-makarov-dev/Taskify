namespace Taskify.WebApi.Extensions;

public static class ConfigurationExtensions
{
    public static T GetSectionOrThrow<T>(this IConfiguration configuration, string sectionName)
    {
        return configuration.GetSection(sectionName).Get<T>() ??
               throw new InvalidOperationException($"The configuration section {sectionName} was not found.");
    }
}