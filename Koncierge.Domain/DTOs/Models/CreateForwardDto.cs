using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs.Models
{
    public class CreateForwardDto
    {

        public KonciergeKubeConfigDto KubeConfig { get; set; }
        public KonciergeContextDto Context { get; set; }
        public KonciergeNamespaceDto Namespace { get; set; }
        public KonciergePodDto Pod { get; set; }
        public KonciergeServiceDto Service { get; set; }
        public List<KonciergePortDto> ToForward { get; set; }=new List<KonciergePortDto>();




    }
}
