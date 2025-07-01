using k8s;
using k8s.Models;
using Koncierge.Core.K8s.Mappers;
using Koncierge.Domain.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Extensions
{
    public static class KonciergeSecretsExtension
    {


        public static async Task<List<KonciergeAdditionalConfigDto>> GetSecretsAsync(this KonciergeClient _kc, string ns = null)
        {
            V1SecretList secList = new V1SecretList();

            secList = string.IsNullOrEmpty(ns)
                ? await _kc.Client.CoreV1.ListSecretForAllNamespacesAsync() // All namespaces
                : await _kc.Client.CoreV1.ListNamespacedSecretAsync(ns); // Specific namespace



            return KonciergeK8sProfile.GetAsmapper().Map<List<KonciergeAdditionalConfigDto>>(secList.Items);

        }


        public static async Task<List<KonciergeAdditionalConfigDto>> GetConfigMapsAsync(this KonciergeClient _kc, string ns = null)
        {
            V1ConfigMapList mapList = new V1ConfigMapList();

            mapList = string.IsNullOrEmpty(ns)
                ? await _kc.Client.CoreV1.ListConfigMapForAllNamespacesAsync() // All namespaces
                : await _kc.Client.CoreV1.ListNamespacedConfigMapAsync(ns); // Specific namespace



            return KonciergeK8sProfile.GetAsmapper().Map<List<KonciergeAdditionalConfigDto>>(mapList.Items);

        }




    }
}
