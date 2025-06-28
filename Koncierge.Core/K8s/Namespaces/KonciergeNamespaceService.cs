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

       

        public async Task<List<string>> GetNamespacesAsync(Guid cfgId)
        {
            var k = await _clientManager.GetClient(cfgId);
            var namespaces = await k.Client.CoreV1.ListNamespaceAsync();
            return namespaces.Items.Select(ns => ns.Metadata.Name).ToList();
        }

        public async Task<List<string>> GetNamespacesAsync(Guid cfgId, string ctx)
        {
            var k = await _clientManager.GetClient(cfgId, ctx);
            var namespaces = await k.Client.CoreV1.ListNamespaceAsync();
            return namespaces.Items.Select(ns => ns.Metadata.Name).ToList();
        }
    }
}
