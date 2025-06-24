using k8s;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s
{
    public class KubernetesClientManager: IKubernetesClientManager, IDisposable
    {
        private readonly ConcurrentDictionary<string, IKubernetes> _clients = new();
       // private readonly ILogger<KubernetesClientManager> _logger;

        public KubernetesClientManager()
        {
            
        }

        public IKubernetes GetClient(string kubeconfigPath)
        {
            return _clients.GetOrAdd(kubeconfigPath, path =>
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(path);
                return new Kubernetes(config);
            });
        }

        public void RemoveClient(string kubeconfigPath)
        {
            if (_clients.TryRemove(kubeconfigPath, out var client))
            {
                (client as IDisposable)?.Dispose();
            }
        }

        public void Dispose()
        {
            foreach (var client in _clients.Values)
            {
                (client as IDisposable)?.Dispose();
            }
            _clients.Clear();
        }

    }
}
