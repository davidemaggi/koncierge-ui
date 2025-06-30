using k8s;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s
{
    public interface IKubernetesClientManager
    {

        ConcurrentBag<KonciergeClient> GetAllClients();
        Task<KonciergeClient> GetClientById(Guid clientId);
        Task<KonciergeClient> GetClient(Guid cfgId);
        //void RemoveClient(string kubeconfigPath);

        Task<KonciergeClient> GetClient(Guid cfgId, string context);
      //  void RemoveClient(string kubeconfigPath, string context);

    }
}
