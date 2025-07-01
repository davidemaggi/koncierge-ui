using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeNamespacedK8sDto: KonciergeBaseK8sDto
    {

        public string Namespace { get; set; }



    }
}
