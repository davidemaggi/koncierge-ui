
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace KonciergeUI.Data
{
    public class PreferencesStorage : IPreferencesStorage
    {
        private const string CurrentThemeKey = "CurrentTheme";
        private const string CurrentLanguageKey = "CurrentLanguage";
        private const string LastSelectedClusterKey = "LastSelectedCluster";
        private const string CustomKubeconfigPathsKey = "CustomKubeconfigPaths";

        public async Task<string?> GetCurrentThemeAsync()
        {
            return Preferences.Get(CurrentThemeKey, "System");
        }

        public async Task SetCurrentThemeAsync(string theme)
        {
            Preferences.Set(CurrentThemeKey, theme);
            await Task.CompletedTask;
        }

        public async Task<string?> GetCurrentLanguageAsync()
        {
            return Preferences.Get(CurrentLanguageKey, "en");
        }

        public async Task SetCurrentLanguageAsync(string language)
        {
            Preferences.Set(CurrentLanguageKey, language);
            await Task.CompletedTask;
        }

        public async Task<string?> GetLastSelectedClusterIdAsync()
        {
            return Preferences.Get(LastSelectedClusterKey, null);
        }

        public async Task SetLastSelectedClusterIdAsync(string clusterId)
        {
            Preferences.Set(LastSelectedClusterKey, clusterId);
            await Task.CompletedTask;
        }

        public async Task<List<string>> GetCustomKubeconfigPathsAsync()
        {
            var json = Preferences.Get(CustomKubeconfigPathsKey, "[]");

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
                Preferences.Set(CustomKubeconfigPathsKey, json);
            }

            await Task.CompletedTask;
        }

        public async Task RemoveCustomKubeconfigPathAsync(string path)
        {
            var paths = await GetCustomKubeconfigPathsAsync();

            if (paths.Remove(path))
            {
                var json = JsonSerializer.Serialize(paths);
                Preferences.Set(CustomKubeconfigPathsKey, json);
            }

            await Task.CompletedTask;
        }
    }
}