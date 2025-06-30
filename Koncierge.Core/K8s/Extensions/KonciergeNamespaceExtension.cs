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
    public static class KonciergeNamespaceExtension
    {

        public async static Task<List<KonciergeNamespaceDto>> GetNamespacesAsync(this KonciergeClient _kc)
        {
        
            var namespaces = await _kc.Client.CoreV1.ListNamespaceAsync();
            return KonciergeK8sProfile.GetAsmapper().Map<List<KonciergeNamespaceDto>>(namespaces.Items);

        }


    }
}
