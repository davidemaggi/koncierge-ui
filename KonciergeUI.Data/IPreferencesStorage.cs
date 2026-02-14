using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KonciergeUI.Models.Forwarding;
using KonciergeUI.Models;

namespace KonciergeUI.Data
{
    public interface IPreferencesStorage
    {
        // Configuration
        Task<KonciergeConfig> GetConfigAsync();
        Task UpdateConfigAsync(KonciergeConfig config);

        // Custom kubeconfig paths (now using Preferences too)
        Task<List<string>> GetCustomKubeconfigPathsAsync();
        Task AddCustomKubeconfigPathAsync(string path);
        Task RemoveCustomKubeconfigPathAsync(string path);
        
        // Forward Templates
        Task AddForwardTemplateAsync(ForwardTemplate template);
        Task UpdateForwardTemplateAsync(ForwardTemplate updatedTemplate);
        Task RemoveForwardTemplateAsync(Guid id);

        Task<List<ForwardTemplate>> GetForwardTemplatesAsync();

        // Reset
        Task ResetAsync();
    }
}
