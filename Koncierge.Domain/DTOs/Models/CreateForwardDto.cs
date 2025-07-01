using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.DTOs.Models
{
    public class CreateForwardDto
    {

        public Guid KubeConfigId { get; set; } = Guid.Empty;
        public string Namespace { get; set; }


    }
}
