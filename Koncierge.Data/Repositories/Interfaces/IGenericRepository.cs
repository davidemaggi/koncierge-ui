using Koncierge.Domain.Entities;
using Koncierge.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Data.Repositories.Interfaces
{
    public interface IGenericRepository<TEntity> where TEntity : class, IBaseEntity
    {

        IQueryable<TEntity> GetAll(bool AsReadOnly = false);

        Task<TEntity?> GetById(Guid id, bool AsReadOnly=false);

        Task Create(TEntity entity);

        Task Update(TEntity entity);
        Task UpdateAll(TEntity[] entities);

        Task Delete(Guid id);
        Task<int> Count();
        Task<bool> Exists(Guid id);

    }
}
