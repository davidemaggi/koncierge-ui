namespace KonciergeUi.Client.Extensions;

public static class ErrorBoundaryExtensions
{
    public static IServiceCollection AddErrorBoundary(this IServiceCollection services)
    {
        // Log unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Console.WriteLine($"Global unhandled: {ex}");
        };

        return services;
    }
}