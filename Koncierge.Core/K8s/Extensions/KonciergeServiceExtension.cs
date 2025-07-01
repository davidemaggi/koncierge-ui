using k8s;
using k8s.Models;
using Koncierge.Core.K8s.Mappers;
using Koncierge.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Extensions
{
    public static class KonciergeServicesExtension
    {



        
        public static async Task<List<KonciergeServiceDto>> GetServicesAsync(this KonciergeClient _kc, string ns = null)
        {
            V1ServiceList svcList = new V1ServiceList();

            svcList = string.IsNullOrEmpty(ns)
                ? await _kc.Client.CoreV1.ListServiceForAllNamespacesAsync() // All namespaces
                : await _kc.Client.CoreV1.ListNamespacedServiceAsync(ns); // Specific namespace



            return KonciergeK8sProfile.GetAsmapper().Map<List<KonciergeServiceDto>>(svcList.Items);

        }



        public static List<V1ServicePort> GetExposedPorts(this V1Service svc)
        {
            var ports = new List<V1ServicePort>();

            if (svc?.Spec?.Ports != null)
            {
                ports = svc.Spec.Ports.ToList();
            }
            return ports;
        }



    }
}
