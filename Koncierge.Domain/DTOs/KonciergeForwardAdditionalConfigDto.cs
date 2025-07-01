using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{


        public class KonciergeForwardAdditionalConfigDto : KonciergeNamespacedK8sDto
        {

        public Guid Id { get; set; }


        public List<KonciergeForwardAdditionalConfigItemDto> Items { get; set; } = new();
            public AdditionalConfigType Type { get; set; } = AdditionalConfigType.Secret;



        }

    public class KonciergeForwardAdditionalConfigItemDto : KonciergeNamespacedK8sDto
    {

        public Guid Id { get; set; }


       public string Value { get; set; }



    }


}
