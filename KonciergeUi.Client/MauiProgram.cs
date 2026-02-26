using KonciergeUi.Client.Extensions;
using KonciergeUi.Client.Services;
using Microsoft.Extensions.Logging;


namespace KonciergeUi.Client;

public static class MauiProgram
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("Italianno-Regular.ttf", "Italianno-Regular");
                fonts.AddFont("Roboto.ttf", "Roboto");
            });


#if DEBUG
        
        builder.Logging.ClearProviders();
        builder.Logging.AddDebug(); // Debug → Rider Debug window
        //builder.Logging.SetMinimumLevel(LogLevel.Trace); 
        
        builder.Services.AddBlazorWebViewDeveloperTools();
        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
            var xxx = args.Exception;
        };

#endif

        builder.Services.AddMauiBlazorWebView();
            



        // Register app services
        builder.Services.RegisterKonciergeServices();
        builder.Services.AddSingleton<IAppVersionProvider, AppVersionProvider>();
        
        var app= builder.Build();

        ServiceProvider = app.Services;

        return app;
    }
}