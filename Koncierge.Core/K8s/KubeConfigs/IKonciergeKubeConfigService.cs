using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Namespaces
{
    public interface IKonciergeKubeConfigService
    {
        string GetDefaultKubeConfigPath();


    }
}
