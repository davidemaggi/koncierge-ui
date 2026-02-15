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
            
            // Log the config path for debugging (can be removed later)
            Console.WriteLine($"[Koncierge] Config file path: {_filePath}");
        }
        
        /// <summary>
        /// Gets the path to the configuration file.
        /// Useful for debugging to verify both CLI and MAUI use the same path.
        /// </summary>
        public string ConfigFilePath => _filePath;

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
            }

            if (!string.IsNullOrWhiteSpace(data.CurrentLanguage))
            {
                data.Config.CurrentLanguage = data.CurrentLanguage;
            }

            if (!string.IsNullOrWhiteSpace(data.LastSelectedClusterId))
            {
                data.Config.LastSelectedClusterId = data.LastSelectedClusterId;
            }

            // Clear all legacy properties after migration
            data.CurrentTheme = null;
            data.CurrentLanguage = null;
            data.LastSelectedClusterId = null;

            // Mark as migrated
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
            // Use a consistent path for both MAUI and CLI applications
            // This ensures both apps share the same configuration
            
            string homePath;
            
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
            {
                // On macOS/MacCatalyst, we need to bypass MAUI sandbox to get the real home directory
                // The sandbox changes HOME to point to the container, but we want the real user home
                
                // First try: Use the real HOME from the original environment (before sandbox)
                // MAUI doesn't always override HOME completely
                var homeEnv = Environment.GetEnvironmentVariable("HOME");
                
                // Check if HOME points to a sandbox container path
                if (!string.IsNullOrEmpty(homeEnv) && !homeEnv.Contains("/Library/Containers/"))
                {
                    // HOME is not sandboxed, use it directly
                    homePath = homeEnv;
                }
                else
                {
                    // HOME is sandboxed or not set, construct the real home path
                    // Try multiple methods to get the username
                    var user = Environment.UserName;
                    
                    // Environment.UserName should still return the real username even in sandbox
                    if (!string.IsNullOrEmpty(user))
                    {
                        var realHome = $"/Users/{user}";
                        if (Directory.Exists(realHome))
                        {
                            homePath = realHome;
                        }
                        else
                        {
                            // Fallback: extract from sandboxed HOME path
                            // Sandbox HOME looks like: /Users/<user>/Library/Containers/<bundle-id>/Data
                            if (!string.IsNullOrEmpty(homeEnv) && homeEnv.StartsWith("/Users/"))
                            {
                                var parts = homeEnv.Split('/');
                                if (parts.Length >= 3)
                                {
                                    homePath = $"/Users/{parts[2]}";
                                }
                                else
                                {
                                    homePath = homeEnv; // Last resort
                                }
                            }
                            else
                            {
                                homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            }
                        }
                    }
                    else
                    {
                        homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    }
                }
            }
            else if (OperatingSystem.IsWindows())
            {
                // On Windows, use USERPROFILE which is consistent
                homePath = Environment.GetEnvironmentVariable("USERPROFILE") 
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else
            {
                // On Linux, use HOME environment variable
                homePath = Environment.GetEnvironmentVariable("HOME") 
                    ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            
            if (string.IsNullOrWhiteSpace(homePath))
            {
                // Last resort fallback
                homePath = ".";
            }

            // Use ~/.koncierge as the config directory (works on all platforms)
            var targetPath = Path.Combine(homePath, ".koncierge");
            Directory.CreateDirectory(targetPath);
            return targetPath;
        }
    }
}
