using Koncierge.Domain.Entities;

namespace Koncierge.Data.Repositories.Interfaces;

public interface IKubeForwardRepository : IGenericRepository<KonciergeForward>
{
    
    IQueryable<KonciergeForward> GetAllWithInclude();
    IQueryable<KonciergeForward> GetAllWithIncludeForConfig(Guid confId, string context);
}