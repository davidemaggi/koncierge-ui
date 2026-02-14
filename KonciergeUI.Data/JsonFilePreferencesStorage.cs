using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KonciergeUI.Data
{
    public class JsonFilePreferencesStorage : IPreferencesStorage
    {
        private readonly string _filePath;
        private readonly object _lock = new();
        private PreferencesData _data;

        private class PreferencesData
        {
            public KonciergeConfig Config { get; set; } = new();

            // Migration tracking - if true, legacy properties have been migrated
            public bool LegacyConfigMigrated { get; set; } = false;

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? CurrentTheme { get; set; } = "System";

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? CurrentLanguage { get; set; } = "en";

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? LastSelectedClusterId { get; set; }

            public List<string> CustomKubeconfigPaths { get; set; } = new();
            public List<ForwardTemplate> ForwardTemplates { get; set; } = new();
        }

        public JsonFilePreferencesStorage()
        {
            var appDataDir = ResolveAppDataDirectory();
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
                    var data = JsonSerializer.Deserialize<PreferencesData>(json) ?? new PreferencesData();
                    MigrateLegacyConfig(data);
                    return data;
                }
                catch
                {
                    // If corrupted, start fresh
                    return new PreferencesData();
                }
            }
        }

        private void MigrateLegacyConfig(PreferencesData data)
        {
            // Skip if already migrated
            if (data.LegacyConfigMigrated)
            {
                return;
            }

            if (data.Config == null)
            {
                data.Config = new KonciergeConfig();
            }

            // Migrate legacy properties if they exist
            if (!string.IsNullOrWhiteSpace(data.CurrentTheme))
            {
                data.Config.CurrentTheme = data.CurrentTheme;
                data.CurrentTheme = null;
            }

            if (!string.IsNullOrWhiteSpace(data.CurrentLanguage))
            {
                data.Config.CurrentLanguage = data.CurrentLanguage;
                data.CurrentLanguage = null;
            }

            if (!string.IsNullOrWhiteSpace(data.LastSelectedClusterId))
            {
                data.Config.LastSelectedClusterId = data.LastSelectedClusterId;
                data.LastSelectedClusterId = null;
            }

            // Mark as migrated and save
            data.LegacyConfigMigrated = true;
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

        // Configuration
        public Task<KonciergeConfig> GetConfigAsync()
        {
            return Task.FromResult(_data.Config.Copy());
        }

        public async Task UpdateConfigAsync(KonciergeConfig config)
        {
            _data.Config = config.Copy();
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

        public async Task ResetAsync()
        {
            _data = new PreferencesData();
            await SaveToFileAsync();
        }

        private static string ResolveAppDataDirectory()
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".koncierge");
            }

            var targetPath = Path.Combine(basePath, "koncierge");
            Directory.CreateDirectory(targetPath);
            return targetPath;
        }
    }
}
