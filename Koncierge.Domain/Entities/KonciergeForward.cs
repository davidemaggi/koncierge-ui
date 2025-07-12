using Koncierge.Domain.Entities.Common;
using Koncierge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.Entities
{
    public class KonciergeForward : BaseEntity
    {
        public string Protocol { get; set; }
        public int ContainerPort { get; set; }
        public int HostPort { get; set; }
        public int LocalPort { get; set; } 
       public string TargetName { get; set; }

        public required FwdTargetType TargetType { get; set; } = FwdTargetType.SERVICE;
        

        public ICollection<KonciergeForwardAdditionalConfig> AdditionalConfigs { get; set; } = new List<KonciergeForwardAdditionalConfig>();


    }
}
