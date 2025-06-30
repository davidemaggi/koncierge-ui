using Koncierge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s.Forwards
{
    public class PortForwardSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public KonciergeKubeConfig KubeConfig { get; init; }
        public string ContextName { get; init; }
        public string Namespace { get; init; }
        public string TargetName { get; init; }
        public int LocalPort { get; init; }
        public int TargetPort { get; init; }
        public PortForwardStatus Status { get; set; } = PortForwardStatus.Running;
        public MemoryStream Logs { get; } = new MemoryStream();
        public CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();
        public Task ForwardingTask { get; set; }



        public static Guid GenerateId(Guid cfgId, string ctxName, string Namespace, string TargetName, int LocalPort, int TargetPort)
        {


            string combined = $"{cfgId}|{ctxName}|{Namespace}|{TargetName}|{LocalPort}|{TargetPort}";

            // Hash the combined string using SHA-1 (128 bits for GUID)
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(combined));
                Array.Resize(ref hash, 16); // GUIDs are 16 bytes

                return new Guid(hash);
            }


        }


        public enum PortForwardStatus { Running, Stopped, Error }
    }

}
