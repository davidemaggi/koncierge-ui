using System.IO;
using Spectre.Console;

namespace KonciergeUI.Cli.Helpers;

internal static class AsciiLogo
{
    private const string DefaultLogo = @"
  _  __                          _             
 | |/ /___  _ __   ___  ___ _ __(_)_ __   __ _ 
 | ' // _ \\| '_ \\ / __|/ __| '__| | '_ \\ / _` |
 | . \\ (_) | | | | (__| (__| |  | | | | | (_| |
 |_|\\_\\___/|_| |_|\\___|\\___|_|  |_|_| |_|\\__, |
                                        |___/ 
";

    public static void Write()
    {
        var logo = TryLoadLogoFile() ?? DefaultLogo;
        Console.WriteLine(logo.TrimEnd());
    }

    private static string? TryLoadLogoFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "logo.txt"),
            Path.Combine(baseDir, "Resources", "logo.txt"),
            Path.Combine(Directory.GetCurrentDirectory(), "KonciergeUI.Cli", "Resources", "logo.txt")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        return null;
    }
}
