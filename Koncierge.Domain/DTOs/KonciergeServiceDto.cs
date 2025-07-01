using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeServiceDto : KonciergeNamespacedK8sDto, IKonciergeBaseK8sDto, IForwardableDto
    {
        public List<KonciergePortDto> Ports { get; set; } = new List<KonciergePortDto>();
    }
}
