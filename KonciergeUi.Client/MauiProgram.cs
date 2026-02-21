using KonciergeUi.Client.Extensions;
using KonciergeUi.Client.Services;
using KonciergeUi.Client.State;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
#endif

namespace KonciergeUi.Client;

public static class MauiProgram
{
    public static IServiceProvider ServiceProvider { get; private set; }
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
            ;

#if WINDOWS
        // Enable HTML5 drag/drop inside WebView2 on Windows.
        BlazorWebViewHandler.Mapper.AppendToMapping("AllowDrop", (handler, view) =>
        {
            if (handler.PlatformView is WebView2 webView)
            {
                webView.AllowDrop = true;
            }
        });
#endif

        // Register app services
        builder.Services.RegisterKonciergeServices();
        builder.Services.AddSingleton<IAppVersionProvider, AppVersionProvider>();
        
        var app= builder.Build();

        ServiceProvider = app.Services;

        return app;
    }
}