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
    public class KubeCForwardRepository : GenericRepository<KonciergeForward>, IKubeForwardRepository
    {

        private readonly KonciergeDbContext _ctx;
        public KubeCForwardRepository(KonciergeDbContext ctx) : base(ctx)

        {
            _ctx = ctx;
        }

        public IQueryable<KonciergeForward> GetAllWithInclude()
        {
            var ret = _ctx.Set<KonciergeForward>().Include(x=>x.AdditionalConfigs).Include(x=>x.AdditionalConfigs);
            return ret;
        }

        public IQueryable<KonciergeForward> GetAllWithIncludeForConfig(Guid confId, string context)
        {
            throw new NotImplementedException();
        }
    }
}
