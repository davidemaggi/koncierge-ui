using System.Linq;
using System.Reflection;

namespace KonciergeUI.Cli.Infrastructure;

internal static class VersionInfo
{
    public static string Description { get; } = "Kubernetes Port Forward Manager";
    public static string Channel { get; } = "main";
    public static string DisplayVersion { get; } = GetDisplayVersion();
    public static string BuildVersion { get; } = GetBuildVersion();

    private static string GetDisplayVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var informationalVersion = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion;
        }

        var version = assembly?.GetName().Version;
        return version?.ToString() ?? "unknown";
    }

    private static string GetBuildVersion()
    {
        var digitsOnly = new string(DisplayVersion.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digitsOnly) ? DisplayVersion : digitsOnly;
    }
}
