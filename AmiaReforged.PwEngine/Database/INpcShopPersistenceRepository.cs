using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Database;

public interface INpcShopPersistenceRepository
{
    Task<IReadOnlyList<NpcShopRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<NpcShopRecord?> GetByTagAsync(string tag, CancellationToken cancellationToken = default);

    Task UpsertAsync(NpcShopRecord shop, CancellationToken cancellationToken = default);

    Task RemoveMissingProductsAsync(long shopId, IReadOnlyCollection<string> definedResRefs, CancellationToken cancellationToken = default);
}
