using KonciergeUi.Client.State;
using KonciergeUI.Translations.Services;

namespace KonciergeUi.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterKonciergeServices(this IServiceCollection services)
    {
        // State
        services.AddSingleton<UiState>();
        services.AddSingleton<ForwardSessionState>();

        // Translations
        services.AddSingleton<ILocalizationService, LocalizationService>();

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