using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(INpcShopPersistenceRepository))]
public sealed class NpcShopPersistenceRepository(PwContextFactory contextFactory) : INpcShopPersistenceRepository
{
    public async Task<IReadOnlyList<NpcShopRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        List<NpcShopRecord> shops = await ctx.NpcShops
            .Include(s => s.Products)
            .Include(s => s.VaultItems)
            .ToListAsync(cancellationToken);

        return shops;
    }

    public async Task<NpcShopRecord?> GetByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        return await ctx.NpcShops
            .Include(s => s.Products)
            .Include(s => s.VaultItems)
            .Include(s => s.LedgerEntries)
            .FirstOrDefaultAsync(s => s.Tag == tag, cancellationToken);
    }

    public async Task UpsertAsync(NpcShopRecord shop, CancellationToken cancellationToken = default)
    {
        if (shop is null)
        {
            throw new ArgumentNullException(nameof(shop));
        }

        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        NpcShopRecord? existing = await ctx.NpcShops
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Tag == shop.Tag, cancellationToken);

        if (existing is null)
        {
            ctx.NpcShops.Add(shop);
        }
        else
        {
            existing.DisplayName = shop.DisplayName;
            existing.ShopkeeperTag = shop.ShopkeeperTag;
            existing.Description = shop.Description;
            existing.RestockMinMinutes = shop.RestockMinMinutes;
            existing.RestockMaxMinutes = shop.RestockMaxMinutes;
            existing.NextRestockUtc = shop.NextRestockUtc;
            existing.DefinitionHash = shop.DefinitionHash;
            existing.UpdatedAt = DateTime.UtcNow;

            SyncProducts(existing, shop.Products);
        }

        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveMissingProductsAsync(
        long shopId,
        IReadOnlyCollection<string> definedResRefs,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        IQueryable<NpcShopProductRecord> products = ctx.NpcShopProducts.Where(p => p.ShopId == shopId);

        if (definedResRefs.Count > 0)
        {
            products = products.Where(p => !definedResRefs.Contains(p.ResRef));
        }

        ctx.NpcShopProducts.RemoveRange(await products.ToListAsync(cancellationToken));
        await ctx.SaveChangesAsync(cancellationToken);
    }

    private static void SyncProducts(NpcShopRecord existing, IReadOnlyCollection<NpcShopProductRecord> incoming)
    {
        Dictionary<string, NpcShopProductRecord> existingByResRef = existing.Products
            .ToDictionary(p => p.ResRef, StringComparer.OrdinalIgnoreCase);

        foreach (NpcShopProductRecord incomingProduct in incoming)
        {
            if (existingByResRef.TryGetValue(incomingProduct.ResRef, out NpcShopProductRecord? record))
            {
                record.Price = incomingProduct.Price;
                record.MaxStock = incomingProduct.MaxStock;
                record.RestockAmount = incomingProduct.RestockAmount;
                record.SortOrder = incomingProduct.SortOrder;
                record.LocalVariablesJson = incomingProduct.LocalVariablesJson;
                record.AppearanceJson = incomingProduct.AppearanceJson;
                record.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing.Products.Add(new NpcShopProductRecord
                {
                    ResRef = incomingProduct.ResRef,
                    Price = incomingProduct.Price,
                    MaxStock = incomingProduct.MaxStock,
                    RestockAmount = incomingProduct.RestockAmount,
                    SortOrder = incomingProduct.SortOrder,
                    LocalVariablesJson = incomingProduct.LocalVariablesJson,
                    AppearanceJson = incomingProduct.AppearanceJson,
                    CurrentStock = incomingProduct.CurrentStock,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
    }
}
