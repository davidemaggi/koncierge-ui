using KonciergeUI.Models.Forwarding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace KonciergeUI.Data
{
    public class JsonFilePreferencesStorage : IPreferencesStorage
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private PreferencesData _data;

        private class PreferencesData
        {
            public string CurrentTheme { get; set; } = "System";
            public string CurrentLanguage { get; set; } = "en";
            public string? LastSelectedClusterId { get; set; }
            public List<string> CustomKubeconfigPaths { get; set; } = new();
            public List<ForwardTemplate> ForwardTemplates { get; set; } = new();
        }

        public JsonFilePreferencesStorage()
        {
            // Get AppData directory - works on both Windows and macOS
            var appDataDir = FileSystem.Current.AppDataDirectory;

            // Create koncierge folder
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }

            _filePath = Path.Combine(appDataDir, "koncierge_preferences.json");
            _data = LoadFromFile();
        }

        private PreferencesData LoadFromFile()
        {
            lock (_lock)
            {
                if (!File.Exists(_filePath))
                {
                    return new PreferencesData();
                }

                try
                {
                    var json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<PreferencesData>(json) ?? new PreferencesData();
                }
                catch
                {
                    // If corrupted, start fresh
                    return new PreferencesData();
                }
            }
        }

        private async Task SaveToFileAsync()
        {
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            lock (_lock)
            {
                File.WriteAllText(_filePath, json);
            }

            await Task.CompletedTask;
        }

        // Theme methods
        public async Task<string?> GetCurrentThemeAsync()
        {
            return _data.CurrentTheme;
        }

        public async Task SetCurrentThemeAsync(string theme)
        {
            _data.CurrentTheme = theme;
            await SaveToFileAsync();
        }

        // Language methods
        public async Task<string?> GetCurrentLanguageAsync()
        {
            return _data.CurrentLanguage;
        }

        public async Task SetCurrentLanguageAsync(string language)
        {
            _data.CurrentLanguage = language;
            await SaveToFileAsync();
        }

        // Cluster methods
        public async Task<string?> GetLastSelectedClusterIdAsync()
        {
            return _data.LastSelectedClusterId;
        }

        public async Task SetLastSelectedClusterIdAsync(string clusterId)
        {
            _data.LastSelectedClusterId = clusterId;
            await SaveToFileAsync();
        }

        // Kubeconfig paths methods
        public async Task<List<string>> GetCustomKubeconfigPathsAsync()
        {
            return new List<string>(_data.CustomKubeconfigPaths);
        }

        public async Task AddCustomKubeconfigPathAsync(string path)
        {
            if (!_data.CustomKubeconfigPaths.Contains(path))
            {
                _data.CustomKubeconfigPaths.Add(path);
                await SaveToFileAsync();
            }
        }

        public async Task RemoveCustomKubeconfigPathAsync(string path)
        {
            if (_data.CustomKubeconfigPaths.Remove(path))
            {
                await SaveToFileAsync();
            }
        }

        // Forward templates methods
        public async Task<List<ForwardTemplate>> GetForwardTemplatesAsync()
        {
            return new List<ForwardTemplate>(_data.ForwardTemplates);
        }

        public async Task AddForwardTemplateAsync(ForwardTemplate template)
        {
            var existingIndex = _data.ForwardTemplates.FindIndex(t => t.Id == template.Id);

            if (existingIndex == -1)
            {
                template.CreatedAt = DateTimeOffset.UtcNow;
                template.ModifiedAt = template.CreatedAt;
                _data.ForwardTemplates.Add(template);
                await SaveToFileAsync();
            }
            else
            {
                await UpdateForwardTemplateAsync(template);
            }
        }

        public async Task UpdateForwardTemplateAsync(ForwardTemplate updatedTemplate)
        {
            var existingIndex = _data.ForwardTemplates.FindIndex(t => t.Id == updatedTemplate.Id);

            if (existingIndex != -1)
            {
                updatedTemplate.ModifiedAt = DateTimeOffset.UtcNow;
                _data.ForwardTemplates[existingIndex] = updatedTemplate;
                await SaveToFileAsync();
            }
        }

        public async Task RemoveForwardTemplateAsync(Guid id)
        {
            if (_data.ForwardTemplates.RemoveAll(t => t.Id == id) > 0)
            {
                await SaveToFileAsync();
            }
        }
    }
}
