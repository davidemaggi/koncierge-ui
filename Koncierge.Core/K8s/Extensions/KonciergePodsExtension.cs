using k8s;
using k8s.Models;
using Koncierge.Core.K8s.Mappers;
using Koncierge.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Koncierge.Core.K8s.Extensions
{
    public static class KonciergePodsExtension
    {

        public static async Task<List<KonciergePodDto>> GetPodsAsync(this KonciergeClient _kc, string ns = null)
        {
            V1PodList podList = new V1PodList();

            podList= string.IsNullOrEmpty(ns)
                ? await _kc.Client.CoreV1.ListPodForAllNamespacesAsync() // All namespaces
                : await _kc.Client.CoreV1.ListNamespacedPodAsync(ns); // Specific namespace



            return KonciergeK8sProfile.GetAsmapper().Map<List<KonciergePodDto>>(podList.Items);

        }



        public static List<V1ContainerPort> GetExposedPorts(this V1Pod pod)
        {
            var ports = new List<V1ContainerPort>();

            if (pod?.Spec?.Containers != null)
            {
                foreach (var container in pod.Spec.Containers)
                {
                    if (container.Ports != null)
                    {
                        ports=container.Ports.ToList();
                    }
                }
            }
            return ports;
        }



    }
}
