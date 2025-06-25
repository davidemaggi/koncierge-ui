using Koncierge.Data.Repositories.Interfaces;
using Koncierge.Domain.Entities;
using Koncierge.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Koncierge.Data.Repositories.Implementations
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity>
       where TEntity : class, IBaseEntity
    {
        private readonly KonciergeDbContext _dbContext;

        public GenericRepository(KonciergeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<TEntity> GetAll(bool AsReadOnly = false)
        {
            return AsReadOnly ? _dbContext.Set<TEntity>().AsNoTracking() : _dbContext.Set<TEntity>();
        }

        public async Task<TEntity?> GetById(Guid id, bool AsReadOnly = false)
        {
            return AsReadOnly ? await _dbContext.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id) : await _dbContext.Set<TEntity>().FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task Create(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAll(TEntity[] entities)
        {
            _dbContext.Set<TEntity>().UpdateRange(entities);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Guid id)
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync(id);
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(Guid id)
        {
            return await _dbContext.Set<TEntity>().AnyAsync(e => e.Id == id);
        }

        public async Task<int> Count()
        {
            return await _dbContext.Set<TEntity>().CountAsync();
        }
    }
}
