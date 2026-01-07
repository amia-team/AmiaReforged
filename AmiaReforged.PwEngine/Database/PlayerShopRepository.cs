using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IPlayerShopRepository))]
public class PlayerShopRepository(PwContextFactory factory) : IPlayerShopRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void AddStall(PlayerStall newStall)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.PlayerStalls.Add(newStall);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }


    public List<StallProduct>? ProductsForShop(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.StallProducts
            .Where(p => p.StallId == shopId)
            .ToList();
    }

    public StallProduct? GetProductById(long stallId, long productId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.StallProducts.SingleOrDefault(p => p.Id == productId && p.StallId == stallId);
    }

    public void AddProductToShop(long shopId, StallProduct product)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            product.StallId = shopId;
            ctx.StallProducts.Add(product);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void RemoveProductFromShop(long shopId, long productId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        using IDbContextTransaction transaction = ctx.Database.BeginTransaction();

        try
        {
            StallProduct? product = ctx.StallProducts.SingleOrDefault(p => p.Id == productId && p.StallId == shopId);
            if (product == null)
            {
                return;
            }

            List<StallTransaction> relatedTransactions = ctx.StallTransactions
                .Where(t => t.StallProductId == productId)
                .ToList();

            if (relatedTransactions.Count > 0)
            {
                foreach (StallTransaction transactionRecord in relatedTransactions)
                {
                    transactionRecord.StallProductId = null;
                }
            }

            ctx.StallProducts.Remove(product);
            ctx.SaveChanges();
            transaction.Commit();
        }
        catch (Exception e)
        {
            Log.Error(e);
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackError)
            {
                Log.Warn(rollbackError, "Failed to rollback stall product removal transaction.");
            }
        }
    }

    public bool IsShopOwner(Guid characterId, long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        PlayerStall? shop = ctx.PlayerStalls.Find(shopId);
        if (shop == null) return false;

        return shop.OwnerCharacterId == characterId;
    }

    public void CreateShop(PlayerStall shop)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.PlayerStalls.Add(shop);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void DeleteShop(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            PlayerStall? shop = ctx.PlayerStalls.Find(shopId);
            if (shop == null) return;
            ctx.PlayerStalls.Remove(shop);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public void UnownShop(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            PlayerStall? shop = ctx.PlayerStalls.Find(shopId);
            if (shop == null) return;
            shop.OwnerCharacterId = null;
            shop.OwnerPersonaId = null;
            shop.OwnerDisplayName = null;
            shop.CoinHouseAccountId = null;
            shop.HoldEarningsInStall = false;
            // Reset current tenure tracking for new owner
            shop.CurrentTenureGrossSales = 0;
            shop.CurrentTenureNetEarnings = 0;
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public bool UpdateShop(long stallId, Action<PlayerStall> updateAction)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            PlayerStall? stall = ctx.PlayerStalls.SingleOrDefault(s => s.Id == stallId);
            if (stall == null)
            {
                return false;
            }

            updateAction(stall);
            stall.UpdatedUtc = DateTime.UtcNow;

            ctx.SaveChanges();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            return false;
        }
    }

    public bool UpdateShopWithMembers(long stallId, Action<PlayerStall> updateAction, IEnumerable<PlayerStallMember> members)
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        using IDbContextTransaction transaction = ctx.Database.BeginTransaction();

        try
        {
            PlayerStall? stall = ctx.PlayerStalls.SingleOrDefault(s => s.Id == stallId);
            if (stall == null)
            {
                return false;
            }

            updateAction(stall);
            stall.UpdatedUtc = DateTime.UtcNow;

            List<PlayerStallMember> existing = ctx.PlayerStallMembers
                .Where(m => m.StallId == stallId)
                .ToList();

            if (existing.Count > 0)
            {
                ctx.PlayerStallMembers.RemoveRange(existing);
            }

            List<PlayerStallMember> newMembers = members?
                .Select(member =>
                {
                    member.StallId = stallId;
                    return member;
                })
                .ToList() ?? new List<PlayerStallMember>();

            if (newMembers.Count > 0)
            {
                ctx.PlayerStallMembers.AddRange(newMembers);
            }

            ctx.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackError)
            {
                Log.Warn(rollbackError, "Failed to rollback stall update transaction.");
            }

            return false;
        }
    }

    public bool UpdateStallAndProduct(long stallId, long productId, Func<PlayerStall, StallProduct, bool> updateAction)
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        using IDbContextTransaction transaction = ctx.Database.BeginTransaction();

        try
        {
            PlayerStall? stall = ctx.PlayerStalls.SingleOrDefault(s => s.Id == stallId);
            if (stall == null)
            {
                return false;
            }

            StallProduct? product = ctx.StallProducts.SingleOrDefault(p => p.Id == productId && p.StallId == stallId);
            if (product == null)
            {
                return false;
            }

            bool shouldPersist = updateAction(stall, product);
            if (!shouldPersist)
            {
                transaction.Rollback();
                return false;
            }

            stall.UpdatedUtc = DateTime.UtcNow;
            product.UpdatedUtc = DateTime.UtcNow;

            ctx.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e);
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackError)
            {
                Log.Warn(rollbackError, "Failed to rollback stall/product update transaction.");
            }

            return false;
        }
    }

    public bool HasActiveOwnershipInArea(Guid ownerCharacterId, string areaResRef, long excludingStallId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.Any(s =>
            s.AreaResRef == areaResRef &&
            s.OwnerCharacterId == ownerCharacterId &&
            s.IsActive &&
            s.Id != excludingStallId);
    }

    public PlayerStall? GetShopById(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.Find(shopId);
    }

    public PlayerStall? GetShopWithMembers(long stallId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls
            .Include(s => s.Members)
            .Include(s => s.LedgerEntries)
            .SingleOrDefault(s => s.Id == stallId);
    }

    public List<PlayerStall> AllShops()
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.ToList();
    }

    public List<PlayerStall> StallsForPlayer(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.Where(s => s.Id == shopId).ToList();
    }

    public List<PlayerStall> ShopsByTag(string tag)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.Where(s => s.Tag == tag).ToList();
    }

    public bool StallExists((string tag, string resRef) identifier)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.PlayerStalls.Any(s => s.Tag == identifier.tag && s.AreaResRef == identifier.resRef);
    }

    public List<StallTransaction>? TransactionsForShop(long shopId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.StallTransactions.Where(t => t.StallId == shopId).ToList();
    }

    public List<StallTransaction>? TransactionsForStallWhenOwnedBy(long shopId, Guid ownerId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

    return (from transaction in ctx.StallTransactions
        join stall in ctx.PlayerStalls on transaction.StallId equals stall.Id
        where transaction.StallId == shopId && stall.OwnerCharacterId == ownerId
        select transaction)
        .ToList();
    }

    public void SaveTransaction(StallTransaction transaction)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.StallTransactions.Add(transaction);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public IReadOnlyList<PlayerStallLedgerEntry> GetLedgerEntries(long stallId, Guid? ownerCharacterId, int maxEntries)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        IQueryable<PlayerStallLedgerEntry> query = ctx.PlayerStallLedgerEntries
            .AsNoTracking()
            .Where(entry => entry.StallId == stallId);

        // Filter by owner if specified
        if (ownerCharacterId.HasValue)
        {
            query = query.Where(entry => entry.OwnerCharacterId == ownerCharacterId.Value);
        }

        query = query
            .OrderByDescending(entry => entry.OccurredUtc)
            .ThenByDescending(entry => entry.Id);

        if (maxEntries > 0)
        {
            query = query.Take(maxEntries);
        }

        return query.ToList();
    }

    public void AddLedgerEntry(PlayerStallLedgerEntry entry)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.PlayerStallLedgerEntries.Add(entry);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to persist player stall ledger entry for stall {StallId}.", entry.StallId);
        }
    }

    public List<StallProduct> GetProductsWithoutItemType()
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.StallProducts
            .Where(p => p.BaseItemType == null)
            .ToList();
    }

    public bool UpdateProductBaseItemType(long productId, int baseItemType)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            StallProduct? product = ctx.StallProducts.SingleOrDefault(p => p.Id == productId);
            if (product is null)
            {
                return false;
            }

            product.BaseItemType = baseItemType;
            ctx.SaveChanges();
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to update base item type for product {ProductId}.", productId);
            return false;
        }
    }
}

public interface IPlayerShopRepository
{
    List<StallProduct>? ProductsForShop(long shopId);

    StallProduct? GetProductById(long stallId, long productId);

    void AddProductToShop(long shopId, StallProduct product);

    void RemoveProductFromShop(long shopId, long productId);

    bool IsShopOwner(Guid characterId, long shopId);

    void CreateShop(PlayerStall shop);
    void DeleteShop(long shopId);

    PlayerStall? GetShopById(long shopId);
    PlayerStall? GetShopWithMembers(long stallId);
    List<PlayerStall> AllShops();

    List<PlayerStall> StallsForPlayer(long shopId);

    List<PlayerStall> ShopsByTag(string tag);

    bool StallExists((string tag, string resRef) identifier);

    void UnownShop(long shopId);
    void AddStall(PlayerStall newStall);

    bool UpdateShop(long stallId, Action<PlayerStall> updateAction);

    bool UpdateShopWithMembers(long stallId, Action<PlayerStall> updateAction, IEnumerable<PlayerStallMember> members);

    bool UpdateStallAndProduct(long stallId, long productId, Func<PlayerStall, StallProduct, bool> updateAction);

    bool HasActiveOwnershipInArea(Guid ownerCharacterId, string areaResRef, long excludingStallId);

    List<StallTransaction>? TransactionsForShop(long shopId);
    List<StallTransaction>? TransactionsForStallWhenOwnedBy(long shopId, Guid ownerId);
    void SaveTransaction(StallTransaction transaction);
    IReadOnlyList<PlayerStallLedgerEntry> GetLedgerEntries(long stallId, Guid? ownerCharacterId, int maxEntries);
    void AddLedgerEntry(PlayerStallLedgerEntry entry);

    /// <summary>
    /// Gets all stall products that have a null BaseItemType, for backfill purposes.
    /// </summary>
    List<StallProduct> GetProductsWithoutItemType();

    /// <summary>
    /// Updates the BaseItemType for a specific product.
    /// </summary>
    bool UpdateProductBaseItemType(long productId, int baseItemType);
}
