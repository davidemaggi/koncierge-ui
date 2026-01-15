using KonciergeUi.Client.State;
using KonciergeUI.Core.Clusters;
using KonciergeUI.Translations.Services;
using MudBlazor.Services;

namespace KonciergeUi.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterKonciergeServices(this IServiceCollection services)
    {
        
        services.AddMauiBlazorWebView();

       
        
       
        // MudBlazor
        services.AddMudServices();

        // Localization
        services.AddLocalization();
        
        // State
        services.AddSingleton<UiState>();
        services.AddSingleton<ForwardSessionState>();

        // Translations
        services.AddSingleton<ILocalizationService, LocalizationService>();


        // Core services - MOCK IMPLEMENTATION
        services.AddSingleton<IClusterDiscoveryService, MockClusterDiscoveryService>();

        // TODO: Add other services when ready


        // Data layer
        //services.AddSingleton<ISecureStore, SecureStore>();
        //services.AddSingleton<IPreferencesStorage, PreferencesStorage>();
        //
        //// Core services
        //services.AddSingleton<IPreferencesService, PreferencesService>();
        //services.AddSingleton<IClusterDiscoveryService, ClusterDiscoveryService>();
        //services.AddSingleton<IPortForwardManager, PortForwardManager>();
        //services.AddSingleton<ISecretLinkService, SecretLinkService>();
        //services.AddSingleton<IForwardLogService, ForwardLogService>();
        //
        //// Kube infrastructure
        //services.AddSingleton<IKubeClientFactory, KubeClientFactory>();
        //services.AddScoped<IKubeRepository, KubeRepository>();
        //services.AddScoped<IKubeSecretRepository, KubeSecretRepository>();

        return services;
    }




}