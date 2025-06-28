using Koncierge.Core.K8s;
using Koncierge.Core.K8s.Contexts;
using Koncierge.Core.K8s.Namespaces;
using Koncierge.Data;
using Koncierge.Data.Repositories.Implementations;
using Koncierge.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


using MudBlazor.Services;


namespace Koncierge.Ui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            builder.Services.AddMudServices();

            builder.Services.AddDbContext<KonciergeDbContext>();


            builder.Services.AddScoped<IKubeConfigRepository, KubeConfigRepository>();

            builder.Services.AddSingleton<IKubernetesClientManager, KubernetesClientManager>();
            builder.Services.AddTransient<IKonciergeNamespaceService, KonciergeNamespaceService>();
            builder.Services.AddTransient<IKonciergeKubeConfigService, KonciergeKubeConfigService>();
            builder.Services.AddTransient<IKonciergeContextService, KonciergeContextService>();




#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();

            AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("********** FE Exception **********");
                System.Diagnostics.Debug.WriteLine(e.Exception);
            };
#endif



            var app= builder.Build();

           // var ctx=app.Services.GetService<KonciergeDbContext>();

            // sctx.Initialize();


            return app;
        }
    }
}
