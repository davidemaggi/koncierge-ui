namespace KonciergeUI.Models;

public class KonciergeConfig
{
    public string CurrentTheme { get; set; } = "System";
    public string CurrentLanguage { get; set; } = "en";
    public string? LastSelectedClusterId { get; set; }

    public KonciergeConfig Copy()
    {
        return new KonciergeConfig
        {
            CurrentTheme = CurrentTheme,
            CurrentLanguage = CurrentLanguage,
            LastSelectedClusterId = LastSelectedClusterId
        };
    }
}