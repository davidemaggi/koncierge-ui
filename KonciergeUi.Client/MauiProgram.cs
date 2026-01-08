using KonciergeUi.Client.Extensions;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;

namespace KonciergeUi.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });


#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
            var xxx = args.Exception;
        };

#endif
        

        // Register app services
        builder.Services.RegisterKonciergeServices();
        
        var app= builder.Build();
        
        return app;
    }
}