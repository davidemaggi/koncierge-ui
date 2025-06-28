using Koncierge.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.Entities
{
    public class KonciergeForwardNamespace : BaseEntity
    {
        public required string Name { get; set; }

        public ICollection<KonciergeForward> Forwards { get; set; } = new List<KonciergeForward>();

    }
}
