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

        public string? Name { get; set; }
        public required string Path { get; set; }

        public ICollection<KonciergeForwardContext> Contexts { get; set; } = new List<KonciergeForwardContext>();


    }
}
