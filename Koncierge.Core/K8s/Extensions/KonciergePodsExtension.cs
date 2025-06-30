using k8s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Extensions
{
    public static class KonciergePodsExtension
    {
        /*
        public async static Task<List<string>> GetNamespacesAsync(this KonciergeClient _kc)
        {
        
            var namespaces = await _kc.Client.CoreV1.ListNamespaceAsync();
            return namespaces.Items.Select(ns => ns.Metadata.Name).ToList();
        }

        */
    }
}
