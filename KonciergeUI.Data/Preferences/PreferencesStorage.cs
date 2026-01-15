using KonciergeUI.Data.Secure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KonciergeUI.Data.Preferences
{
    public class PreferencesStorage : IPreferencesStorage
    {
        private const string CustomKubeconfigPathsKey = "CustomKubeconfigPaths";
        private const string LastSelectedClusterKey = "LastSelectedCluster";
        private const string CurrentThemeKey = "CurrentTheme";
        private const string CurrentLanguageKey = "CurrentLanguage";

        private readonly ISecureStore _secureStore;

        public PreferencesStorage(ISecureStore secureStore)
        {
            _secureStore = secureStore;
        }

        // Kubeconfig paths
        public async Task<List<string>> GetCustomKubeconfigPathsAsync()
        {
            var json = await _secureStore.GetAsync(CustomKubeconfigPathsKey);

            if (string.IsNullOrEmpty(json))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task AddCustomKubeconfigPathAsync(string path)
        {
            var paths = await GetCustomKubeconfigPathsAsync();

            if (!paths.Contains(path))
            {
                paths.Add(path);
                var json = JsonSerializer.Serialize(paths);
                await _secureStore.SetAsync(CustomKubeconfigPathsKey, json);
            }
        }

        public async Task RemoveCustomKubeconfigPathAsync(string path)
        {
            var paths = await GetCustomKubeconfigPathsAsync();

            if (paths.Remove(path))
            {
                var json = JsonSerializer.Serialize(paths);
                await _secureStore.SetAsync(CustomKubeconfigPathsKey, json);
            }
        }

        // Selected cluster
        public async Task<string?> GetLastSelectedClusterIdAsync()
        {
            return await _secureStore.GetAsync(LastSelectedClusterKey);
        }

        public async Task SetLastSelectedClusterIdAsync(string clusterId)
        {
            await _secureStore.SetAsync(LastSelectedClusterKey, clusterId);
        }

        // Theme
        public async Task<string?> GetCurrentThemeAsync()
        {
            return await _secureStore.GetAsync(CurrentThemeKey);
        }

        public async Task SetCurrentThemeAsync(string theme)
        {
            await _secureStore.SetAsync(CurrentThemeKey, theme);
        }

        // Language
        public async Task<string?> GetCurrentLanguageAsync()
        {
            return await _secureStore.GetAsync(CurrentLanguageKey);
        }

        public async Task SetCurrentLanguageAsync(string language)
        {
            await _secureStore.SetAsync(CurrentLanguageKey, language);
        }
    }
}
