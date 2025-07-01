using Koncierge.Domain.Entities;

namespace Koncierge.Data.Repositories.Interfaces;

public interface IKubeForwardRepository : IGenericRepository<KonciergeForward>
{
    
    IQueryable<KonciergeKubeConfig> GetAllWithInclude();
    IQueryable<KonciergeKubeConfig> GetAllWithInclude(Guid? cfgId, string? ctx, string? ns, string? freeTxt, int? port = 0);
    IQueryable<KonciergeForward> GetAllWithIncludeForConfig(Guid confId, string context);
}