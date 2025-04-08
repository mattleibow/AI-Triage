#pragma warning disable SKEXP0110 // Suppress experimental warnings
#pragma warning disable SKEXP0001

using Microsoft.Extensions.Configuration;

public static class ConfigurationExtensions
{
    public static string GetOrThrow(this IConfiguration configuration, string key, string errorName)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new Exception($"{errorName} not found");
        }
        return value;
    }

    public static string GetOrDefault(this IConfiguration configuration, string key, string defaultValue)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
