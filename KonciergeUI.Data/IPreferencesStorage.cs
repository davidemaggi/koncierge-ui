using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KonciergeUI.Models.Forwarding;

namespace KonciergeUI.Data
{
    public interface IPreferencesStorage
    {
        // Theme
        Task<string?> GetCurrentThemeAsync();
        Task SetCurrentThemeAsync(string theme);

        // Language
        Task<string?> GetCurrentLanguageAsync();
        Task SetCurrentLanguageAsync(string language);

        // Selected cluster
        Task<string?> GetLastSelectedClusterIdAsync();
        Task SetLastSelectedClusterIdAsync(string clusterId);

        // Custom kubeconfig paths (now using Preferences too)
        Task<List<string>> GetCustomKubeconfigPathsAsync();
        Task AddCustomKubeconfigPathAsync(string path);
        Task RemoveCustomKubeconfigPathAsync(string path);
        
        // Forward Templates
        Task AddForwardTemplateAsync(ForwardTemplate template);
        Task UpdateForwardTemplateAsync(ForwardTemplate updatedTemplate);
        Task RemoveForwardTemplateAsync(Guid id);

        Task<List<ForwardTemplate>> GetForwardTemplatesAsync();
    }
}
