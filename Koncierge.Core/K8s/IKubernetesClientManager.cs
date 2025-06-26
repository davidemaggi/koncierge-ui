using k8s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s
{
    public interface IKubernetesClientManager
    {

        IKubernetes GetClient(string kubeconfigPath);
        void RemoveClient(string kubeconfigPath);

        IKubernetes GetClient(string kubeconfigPath, string context);
        void RemoveClient(string kubeconfigPath, string context);

    }
}
