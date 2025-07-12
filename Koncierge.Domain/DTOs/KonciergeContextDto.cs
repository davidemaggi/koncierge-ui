using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeContextDto: KonciergeBaseK8sDto, IKonciergeBaseK8sDto
    {
        public string DefaultNamespace { get; set; }





    }
}
