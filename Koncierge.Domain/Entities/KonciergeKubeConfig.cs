using Koncierge.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.Entities
{
    public class KonciergeKubeConfig : BaseEntity
    {

        public required string Path { get; set; }
        public bool IsDefault { get; set; } = false;

        public ICollection<KonciergeContextConfig> Contexts { get; set; } = new List<KonciergeContextConfig>();


    }
}
