using k8s;
using Koncierge.Core.K8s.Contexts;
using Koncierge.Core.K8s.Namespaces;
using Koncierge.Data.Repositories.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Core.K8s
{
    public class KubernetesClientManager: IKubernetesClientManager//, IDisposable
    {
        private readonly ConcurrentBag<KonciergeClient> _clients = new();
       // private readonly ILogger<KubernetesClientManager> _logger;

        private readonly IKubeConfigRepository _kubeConfigRepository;
        private readonly IKonciergeContextService _kcService;

        public KubernetesClientManager(IKubeConfigRepository kubeConfigRepository, IKonciergeContextService kcService)
        {
            _kubeConfigRepository=kubeConfigRepository;
            _kcService = kcService;
        }

        public ConcurrentBag<KonciergeClient> GetAllClients() => _clients;

        public async Task<KonciergeClient> GetClient(Guid cfgId)
        {
            var cfg = await _kubeConfigRepository.GetById(cfgId);

            var ctx = await _kcService.GetCurrentContext(cfg.Path);

            return await GetClient(cfgId, ctx.Data);

        }

        public async Task<KonciergeClient> GetClient(Guid cfgId, string? context)
        {

            if (string.IsNullOrWhiteSpace(context)) {
                return await GetClient(cfgId);
            }

            var cfg = await _kubeConfigRepository.GetById(cfgId);
            if (cfg is null) { 
            //TODO: Manage nullable
            }


            var client = _clients.FirstOrDefault(x => x.Id == KonciergeClient.GenerateId(cfg.Id,context));

            if (client is null)
            {

                client = new KonciergeClient(cfg, context);
                _clients.Add(client);

            }

            return client;

            


        }
        /*
        public void RemoveClient(string kubeconfigPath)
        {
            if (_clients.TryRemove(kubeconfigPath, out var client))
            {
                (client as IDisposable)?.Dispose();
            }
        }

        public void RemoveClient(string kubeconfigPath, string context)
        {
            if (_clients.TryRemove($"{kubeconfigPath}_{context}", out var client))
            {
                (client as IDisposable)?.Dispose();
            }
        }

        public void Dispose()
        {
            foreach (var client in _clients)
            {
                (client as IDisposable)?.Dispose();
            }
            _clients.Clear();
        }
        */
    }
}
