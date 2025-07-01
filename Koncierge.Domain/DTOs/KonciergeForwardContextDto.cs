using Koncierge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeForwardContextDto:KonciergeBaseK8sDto
    {


        public Guid Id { get; set; }

        public ICollection<KonciergeForwardNamespaceDto> Namespaces { get; set; } = new List<KonciergeForwardNamespaceDto>();

    }
}
