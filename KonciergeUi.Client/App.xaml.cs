using System.Globalization;
using KonciergeUi.Client.State;

namespace KonciergeUi.Client;

public partial class App : Application
{
    public App()
    {LoadCultureFromPrefs().Wait();
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage()) { Title = "Koncierge" };
    }
    
    private async Task LoadCultureFromPrefs()
    {
        using var scope = MauiProgram.ServiceProvider.CreateScope();
        var uiState = scope.ServiceProvider.GetRequiredService<UiState>();
        await uiState.LoadPreferencesAsync();
        
        var ci = new CultureInfo(uiState.CurrentLanguage);
        CultureInfo.DefaultThreadCurrentCulture = ci;
        CultureInfo.DefaultThreadCurrentUICulture = ci;
        Thread.CurrentThread.CurrentCulture = ci;
        Thread.CurrentThread.CurrentUICulture = ci;
    }
}