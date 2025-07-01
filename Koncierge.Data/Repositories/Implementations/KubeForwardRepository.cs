using Koncierge.Data.Repositories.Interfaces;
using Koncierge.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Data.Repositories.Implementations
{
    public class KubeForwardRepository : GenericRepository<KonciergeForward>, IKubeForwardRepository
    {

        private readonly KonciergeDbContext _ctx;
        public KubeForwardRepository(KonciergeDbContext ctx) : base(ctx)

        {
            _ctx = ctx;
        }

        public IQueryable<KonciergeKubeConfig> GetAllWithInclude()
        {

            var ret = _ctx.KubeConfigs
                  .Include(x => x.Contexts)
                  .ThenInclude(x => x.Namespaces)
                  .ThenInclude(x => x.Forwards)
                  .ThenInclude(x => x.AdditionalConfigs)
                  .ThenInclude(x => x.Items)
                  ;






            return ret;
        }

        public IQueryable<KonciergeKubeConfig> GetAllWithInclude(Guid? cfgId, string? ctx, string? ns, string? freeTxt, int? port = 0)
        {

            var ret = _ctx.KubeConfigs
                  .Include(x => x.Contexts.Where(c => ctx == null || c.Name == ctx || c.Name == freeTxt))
                  .ThenInclude(x => x.Namespaces.Where(c => ns == null || c.Name == ns || c.Name == freeTxt))
                  .ThenInclude(x => x.Forwards.Where(c => port == 0 || c.TargetName == freeTxt || c.TargetPort == port || c.LocalPort == port))
                  .ThenInclude(x => x.AdditionalConfigs)
                  .ThenInclude(x => x.Items)
                  .Where(x => cfgId == null || x.Id == cfgId)
                  ;

            
            
            
            
            
            return ret;
        }

        public IQueryable<KonciergeForward> GetAllWithIncludeForConfig(Guid confId, string context)
        {
            throw new NotImplementedException();
        }
    }
}
