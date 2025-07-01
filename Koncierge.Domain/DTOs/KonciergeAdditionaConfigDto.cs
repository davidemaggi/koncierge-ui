using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeAdditionalConfigDto: KonciergeNamespacedK8sDto
    {

        public List<KonciergeAdditionalConfigItemDto> Items { get; set; } = new();
        public AdditionalConfigType Type { get; set; } = AdditionalConfigType.Secret;



    }

    public class KonciergeAdditionalConfigItemDto : KonciergeBaseK8sDto
    {

        public string Value { get; set; }



    }


    public enum AdditionalConfigType { 
    
        ConfigMap,
        Secret

    }



}
