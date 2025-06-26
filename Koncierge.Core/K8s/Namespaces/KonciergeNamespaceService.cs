using k8s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Namespaces
{
    public class KonciergeNamespaceService : IKonciergeNamespaceService
    {

        private readonly IKubernetesClientManager _clientManager;

        public KonciergeNamespaceService(IKubernetesClientManager clientManager)
        {
            _clientManager = clientManager;
        }

       

        public async Task<List<string>> GetNamespacesAsync(string kubeconfigPath)
        {
            var client = _clientManager.GetClient(kubeconfigPath);
            var namespaces = await client.CoreV1.ListNamespaceAsync();
            return namespaces.Items.Select(ns => ns.Metadata.Name).ToList();
        }

       
    }
}
