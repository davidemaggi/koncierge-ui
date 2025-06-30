using Koncierge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Forwards
{
    public interface IKonciergeForwardManager
    {

        public PortForwardSession StartPortForward(
           KonciergeClient client,
           string namespaceName,
           string targetName,
           int targetPort,
           int localPort,
           bool isService = false);

    }
}
