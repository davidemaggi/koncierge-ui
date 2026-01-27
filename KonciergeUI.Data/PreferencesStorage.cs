
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KonciergeUI.Models.Forwarding;

namespace KonciergeUI.Data
{
    public class PreferencesStorage : IPreferencesStorage
    {
        private const string CurrentThemeKey = "CurrentTheme";
        private const string CurrentLanguageKey = "CurrentLanguage";
        private const string LastSelectedClusterKey = "LastSelectedCluster";
        private const string CustomKubeconfigPathsKey = "CustomKubeconfigPaths";
        private const string ForwardTemplatesKey = "ForwardTemplates";

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
            var locale= Preferences.Get(CurrentLanguageKey, "en");
            return locale;
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
        
        
        public async Task<List<ForwardTemplate>> GetForwardTemplatesAsync()
        {
            var json = Preferences.Get(ForwardTemplatesKey, "[]");
            try
            {
                return JsonSerializer.Deserialize<List<ForwardTemplate>>(json) ?? new List<ForwardTemplate>();
            }
            catch
            {
                return new List<ForwardTemplate>();
            }
        }

        public async Task AddForwardTemplateAsync(ForwardTemplate template)
        {
            var templates = await GetForwardTemplatesAsync();
            // Avoid dupes by Name (or add ID prop if you want)
            if (!templates.Any(t => t.Id == template.Id))
            {
                template.CreatedAt=DateTimeOffset.UtcNow;
                template.ModifiedAt=template.CreatedAt;
                
                templates.Add(template);
                var json = JsonSerializer.Serialize(templates);
                Preferences.Set(ForwardTemplatesKey, json);
            }

            {
                await UpdateForwardTemplateAsync(template);
            }
            await Task.CompletedTask;
        }

        public async Task UpdateForwardTemplateAsync(ForwardTemplate updatedTemplate)
        {
            var templates = await GetForwardTemplatesAsync();
            var existingIndex = templates.FindIndex(t => t.Id == updatedTemplate.Id);
            if (existingIndex != -1)
            {
                updatedTemplate.ModifiedAt=DateTimeOffset.UtcNow;
                
                
                templates[existingIndex] = updatedTemplate;
                var json = JsonSerializer.Serialize(templates);
                Preferences.Set(ForwardTemplatesKey, json);
            }
            await Task.CompletedTask;
        }

        public async Task RemoveForwardTemplateAsync(Guid id)
        {
            var templates = await GetForwardTemplatesAsync();
            if (templates.RemoveAll(t => t.Id == id) > 0)
            {
                var json = JsonSerializer.Serialize(templates);
                Preferences.Set(ForwardTemplatesKey, json);
            }
            await Task.CompletedTask;
        }

      
    }
}