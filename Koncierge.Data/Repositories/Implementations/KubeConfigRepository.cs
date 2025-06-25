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
    public class KubeConfigRepository : GenericRepository<KonciergeKubeConfig>, IKubeConfigRepository
    {

        private readonly KonciergeDbContext _ctx;
        public KubeConfigRepository(KonciergeDbContext ctx) : base(ctx)

        {
            _ctx = ctx;
        }

        public  bool DefaultExists() => _ctx.KubeConfigs.Any(x=>x.IsDefault);
        

        public Task<KonciergeKubeConfig?> getDefaultKubeconfig(bool asReadonly = false) => asReadonly ? _ctx.KubeConfigs.AsNoTracking().Where(x=>x.IsDefault).FirstOrDefaultAsync() : _ctx.KubeConfigs.Where(x => x.IsDefault).FirstOrDefaultAsync();


    }
}
