using Koncierge.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.Entities
{
    public class KonciergeForwardContext: BaseEntity
    {

        public required string Name { get; set; }


        public ICollection<KonciergeForwardNamespace> Namespaces { get; set; } = new List<KonciergeForwardNamespace>();

    }
}
