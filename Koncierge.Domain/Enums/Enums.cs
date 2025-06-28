using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Domain.Enums
{
    public enum FwdTargetType
    { 
    POD,
    SERVICE
    }



    public enum KonciergeActionResult
    {
        PENDING,
        SUCCESS,
        FAILURE,
        WARNING
    }
}
