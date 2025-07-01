using Koncierge.Domain.Entities;
using Koncierge.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public class KonciergeForwardDto
    {
        public Guid Id { get; set; }

        public int LocalPort { get; set; }
        public int TargetPort { get; set; }
        public string TargetName { get; set; }

        public required FwdTargetType TargetType { get; set; } = FwdTargetType.POD;


        public ICollection<KonciergeAdditionalConfigDto> AdditionalConfigs { get; set; } = new List<KonciergeAdditionalConfigDto>();

    }
}
