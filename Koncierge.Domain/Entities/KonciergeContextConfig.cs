using Koncierge.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.Entities
{
    public class KonciergeContextConfig: BaseEntity
    {

        public required string Name { get; set; }


        public ICollection<KonciergeNamespaceConfig> Namespaces { get; set; } = new List<KonciergeNamespaceConfig>();

    }
}
