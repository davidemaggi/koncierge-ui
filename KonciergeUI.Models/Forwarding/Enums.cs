using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Models.Forwarding
{
    public class Enums
    {


        public enum ResourceType
        {
            Pod,
            Service
        }

        public enum LinkedResourceType
        {
            ConfigMap,
            Secret
        }

        public enum ForwardProtocol
        {
            Tcp,
            Http,
            Https,
            Grpc,
            Custom
        }

        public enum ExecutionStatus
        {
            Starting,
            Running,
            PartiallyRunning, // Some forwards running, some failed
            Stopping,
            Stopped,
            Failed
        }

        public enum ForwardStatus
        {
            Starting,
            Running,
            Reconnecting,
            Stopped,
            Stopping,
            Failed
        }
    }
}
