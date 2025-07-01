using Koncierge.Domain.DTOs;
using Koncierge.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.Entities
{
    public class KonciergeForwardAdditionalConfig : BaseEntity
    {
        public AdditionalConfigType Type { get; set; } = AdditionalConfigType.Secret;
        public string Name { get; set; }
        public List<KonciergeForwardAdditionalConfigItem> Items { get; set; }

    }

    public class KonciergeForwardAdditionalConfigItem : BaseEntity
    {
        public string Name { get; set; }
        public string Value { get; set; }

    }
}
