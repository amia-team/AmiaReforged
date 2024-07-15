using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage;

/// <summary>
/// Handles access and authorization of item storage.
/// </summary>
[ServiceBinding(typeof(StorageService))]
public class StorageService
{
    private readonly PwContextFactory _ctxFactory;

    /// <summary>
    /// This service requires a PwContextFactory to create a database context.
    /// Should be handled by the DI container. Only instantiate for testing.
    /// </summary>
    /// <param name="factory"></param>
    public StorageService(PwContextFactory factory)
    {
        _ctxFactory = factory;
    }

    private async Task<bool> CanAccess(long characterId, long storageId)
    {
        await using PwEngineContext ctx = _ctxFactory.CreateDbContext();
        return await ctx.ItemStorageUsers.AnyAsync(su =>
            su.WorldCharacterId == characterId && su.ItemStorageId == storageId);
    }

    /// <summary>
    /// Gets the storage container if the character has access
    /// </summary>
    /// <param name="characterId">Database ID of the character</param>
    /// <param name="storageId">Database ID of the storage the character wishes to access</param>
    /// <returns> ItemStorage if authorized, null otherwise </returns>
    public async Task<ItemStorage?> GetStorage(long characterId, long storageId)
    {
        bool canAccess = await CanAccess(characterId, storageId);
        if (!canAccess) return null;

        await using PwEngineContext ctx = _ctxFactory.CreateDbContext();
        return await ctx.StorageContainers.Include(i => i.Items).SingleOrDefaultAsync(w => w.Id == storageId);
    }
}