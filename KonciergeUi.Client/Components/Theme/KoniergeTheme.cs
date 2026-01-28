using MudBlazor;


namespace KonciergeUi.Client.Components.Theme
{
    public class KoniergeTheme:MudTheme
    {


        public KoniergeTheme() {


            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Roboto", "Helvetica", "Arial", "sans-serif" },
                }
            };



            PaletteLight = new PaletteLight
            {
                AppbarBackground = "#ffffff",
                // standard stuff...
                Primary = MudBlazor.Colors.Blue.Default,
                // custom color for light mode card
                Background = "#F9FAFB",
                // you can keep default others
                Surface = "#ffffff"
            };
            PaletteDark = new PaletteDark
            {
                Primary = MudBlazor.Colors.Blue.Lighten2,
                // custom color for dark mode card background
                Background = "#121827",
                Surface = "#1e2939",
                AppbarBackground = "#202937"
            };


        }


       



    }
}
