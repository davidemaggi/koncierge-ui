using AutoMapper;
using k8s;
using k8s.KubeConfigModels;
using Koncierge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s
{
    public class KonciergeClient
    {

        public IKubernetes Client { get; set; }
        public string Name { get; set; }
        public Guid Id { get; set; }
        public KonciergeKubeConfig KubeConfig { get; set; }
        public string Context { get; set; }

        public KonciergeClient(KonciergeKubeConfig cfg, string ctxName) {

            var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(cfg.Path, ctxName);
            KubeConfig = cfg;
            Client = new Kubernetes(config);
            Id = GenerateId(cfg.Id, ctxName);
            Name = $"{ctxName}@{cfg.Path}";
            Context = ctxName;
        }


        public static Guid GenerateId(Guid cfgId, string ctxName) {


            string combined = $"{cfgId}|{ctxName}";

            // Hash the combined string using SHA-1 (128 bits for GUID)
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(combined));
                Array.Resize(ref hash, 16); // GUIDs are 16 bytes

                return new Guid(hash);
            }


        }

    }
}
