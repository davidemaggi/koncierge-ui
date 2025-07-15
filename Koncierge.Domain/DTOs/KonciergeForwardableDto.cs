using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs
{
    public interface IForwardableDto
    {

        public List<KonciergePortDto> Ports { get; set; }

    }



    public class KonciergePortDto { 
    
        public int ContainerPort { get; set; }
        public int HostPort { get; set; }
        public int? LocalPort { get; set; }
        public string Protocol { get; set; } = "";

        public List<KonciergeAdditionalConfigDto> AdditionalConfig { get; set; } = new List<KonciergeAdditionalConfigDto>();



    }
}
