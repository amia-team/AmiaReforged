using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Database;

public interface IShopPersistenceRepository
{
    Task<IReadOnlyList<ShopRecord>> GetAllAsync(
        ShopKind? kind = null,
        CancellationToken cancellationToken = default);

    Task<ShopRecord?> GetByTagAsync(
        string tag,
        ShopKind? kind = null,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(ShopRecord shop, CancellationToken cancellationToken = default);

    Task RemoveMissingProductsAsync(
        long shopId,
        IReadOnlyCollection<string> definedResRefs,
        CancellationToken cancellationToken = default);

    Task<bool> TryConsumeStockAsync(
        long shopId,
        long productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task ReturnStockAsync(
        long shopId,
        long productId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task UpdateNextRestockAsync(
        long shopId,
        DateTime? nextRestockUtc,
        CancellationToken cancellationToken = default);

    Task<ShopProductRecord> UpsertPlayerProductAsync(
        long shopId,
        ShopProductRecord product,
        byte[] itemData,
        int quantity,
        string? itemName,
        CancellationToken cancellationToken = default);

    Task StoreVaultItemAsync(
        long shopId,
        string resRef,
        string? itemName,
        byte[] itemData,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<ShopVaultItem?> TakeVaultItemAsync(
        long shopId,
        string resRef,
        CancellationToken cancellationToken = default);
}
