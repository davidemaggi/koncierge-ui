using Koncierge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Koncierge.Data.Repositories.Interfaces
{

    public interface IKubeConfigRepository : IGenericRepository<KonciergeKubeConfig>
    {
        //bool DefaultExists();
      //  Task<KonciergeKubeConfig?> getDefaultKubeconfig(bool asReadonly= false);
        Task<KonciergeKubeConfig> Rename(Guid id, string newname);



    }
}
