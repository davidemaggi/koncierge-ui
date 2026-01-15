using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KonciergeUI.Data.Preferences
{
    public interface IPreferencesStorage
    {
        // Kubeconfig paths
        Task<List<string>> GetCustomKubeconfigPathsAsync();
        Task AddCustomKubeconfigPathAsync(string path);
        Task RemoveCustomKubeconfigPathAsync(string path);

        // Selected cluster
        Task<string?> GetLastSelectedClusterIdAsync();
        Task SetLastSelectedClusterIdAsync(string clusterId);

        // Theme
        Task<string?> GetCurrentThemeAsync();
        Task SetCurrentThemeAsync(string theme);

        // Language
        Task<string?> GetCurrentLanguageAsync();
        Task SetCurrentLanguageAsync(string language);
    }
}
