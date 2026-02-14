using KonciergeUi.Client.State;

namespace KonciergeUi.Client.Components.Theme;

public static class KonciergeColors
{

    public static string GetSummaryCardBackground(UiState ui)
    {
        if (ui.IsDarkMode)
        {
            
            return $"background:#2E2E2E;";

        }

        return $"background:#F9FAFB;";
    }
    
    
  


}