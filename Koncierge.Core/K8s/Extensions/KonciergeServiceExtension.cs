using k8s;
using k8s.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Extensions
{
    public static class KonciergeServicesExtension
    {



        public static async Task<V1PodList> GetPodsAsync(this KonciergeClient _kc, string ns = null)
{
    return string.IsNullOrEmpty(ns)
        ? await _kc.Client.CoreV1.ListPodForAllNamespacesAsync() // All namespaces
        : await _kc.Client.CoreV1.ListNamespacedPodAsync(ns); // Specific namespace
}



    }
}
