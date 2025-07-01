using Koncierge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeForwardNamespaceDto : KonciergeBaseK8sDto
    {

        public Guid Id { get; set; }

        public required string Name { get; set; }

        public ICollection<KonciergeForwardDto> Forwards { get; set; } = new List<KonciergeForwardDto>();

    }
}
