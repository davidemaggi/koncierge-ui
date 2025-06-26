using k8s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Namespaces
{
    public class KonciergeKubeConfigService : IKonciergeKubeConfigService
    {

        private readonly IKubernetesClientManager _clientManager;

        public KonciergeKubeConfigService(IKubernetesClientManager clientManager)
        {
            _clientManager = clientManager;
        }

       

        public  string GetDefaultKubeConfigPath()
        {
            // Check if the KUBECONFIG environment variable is set
            var kubeConfigEnv = Environment.GetEnvironmentVariable("KUBECONFIG");
            if (!string.IsNullOrWhiteSpace(kubeConfigEnv))
            {
                // If multiple paths, return the first one (same as kubectl behavior)
                var separator = OperatingSystem.IsWindows() ? ';' : ':';
                var paths = kubeConfigEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                return paths[0];
            }

            // Otherwise, use the default location based on OS
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string kubeConfigPath = Path.Combine(homeDir, ".kube", "config");
            return kubeConfigPath;
        }


    }
}
