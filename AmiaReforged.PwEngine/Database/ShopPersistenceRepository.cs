using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(IShopPersistenceRepository))]
public sealed class ShopPersistenceRepository(PwContextFactory contextFactory) : IShopPersistenceRepository
{
    public async Task<IReadOnlyList<ShopRecord>> GetAllAsync(
        ShopKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        IQueryable<ShopRecord> query = ctx.Shops
            .Include(s => s.Products)
            .Include(s => s.VaultItems);

        if (kind.HasValue)
        {
            query = query.Where(s => s.Kind == kind.Value);
        }

        List<ShopRecord> shops = await query.ToListAsync(cancellationToken);
        return shops;
    }

    public async Task<ShopRecord?> GetByTagAsync(
        string tag,
        ShopKind? kind = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            throw new ArgumentException("Tag must be provided", nameof(tag));
        }

        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        IQueryable<ShopRecord> query = ctx.Shops
            .Include(s => s.Products)
            .Include(s => s.VaultItems)
            .Include(s => s.LedgerEntries)
            .Where(s => s.Tag == tag);

        if (kind.HasValue)
        {
            query = query.Where(s => s.Kind == kind.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertAsync(ShopRecord shop, CancellationToken cancellationToken = default)
    {
        if (shop is null)
        {
            throw new ArgumentNullException(nameof(shop));
        }

        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        ShopRecord? existing = await ctx.Shops
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Tag == shop.Tag, cancellationToken);

        if (existing is null)
        {
            ctx.Shops.Add(shop);
        }
        else
        {
            UpdateShop(existing, shop);
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

        IQueryable<ShopProductRecord> products = ctx.ShopProducts.Where(p => p.ShopId == shopId);

        if (definedResRefs.Count > 0)
        {
            products = products.Where(p => !definedResRefs.Contains(p.ResRef));
        }

        ctx.ShopProducts.RemoveRange(await products.ToListAsync(cancellationToken));
        await ctx.SaveChangesAsync(cancellationToken);
    }

    private static void UpdateShop(ShopRecord existing, ShopRecord incoming)
    {
        existing.DisplayName = incoming.DisplayName;
        existing.ShopkeeperTag = incoming.ShopkeeperTag;
        existing.Description = incoming.Description;
        existing.Kind = incoming.Kind;
        existing.ManualRestock = incoming.ManualRestock;
        existing.ManualPricing = incoming.ManualPricing;
        existing.OwnerAccountId = incoming.OwnerAccountId;
        existing.OwnerCharacterId = incoming.OwnerCharacterId;
        existing.OwnerDisplayName = incoming.OwnerDisplayName;
        existing.RestockMinMinutes = incoming.RestockMinMinutes;
        existing.RestockMaxMinutes = incoming.RestockMaxMinutes;
        existing.NextRestockUtc = incoming.NextRestockUtc;
        existing.VaultBalance = incoming.VaultBalance;
        existing.DefinitionHash = incoming.DefinitionHash;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static void SyncProducts(ShopRecord existing, IReadOnlyCollection<ShopProductRecord> incoming)
    {
        Dictionary<string, ShopProductRecord> existingByResRef = existing.Products
            .ToDictionary(p => p.ResRef, StringComparer.OrdinalIgnoreCase);

        foreach (ShopProductRecord incomingProduct in incoming)
        {
            if (existingByResRef.TryGetValue(incomingProduct.ResRef, out ShopProductRecord? record))
            {
                record.Price = incomingProduct.Price;
                record.MaxStock = incomingProduct.MaxStock;
                record.RestockAmount = incomingProduct.RestockAmount;
                record.SortOrder = incomingProduct.SortOrder;
                record.LocalVariablesJson = incomingProduct.LocalVariablesJson;
                record.AppearanceJson = incomingProduct.AppearanceJson;
                record.IsPlayerManaged = incomingProduct.IsPlayerManaged;
                record.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing.Products.Add(new ShopProductRecord
                {
                    ResRef = incomingProduct.ResRef,
                    Price = incomingProduct.Price,
                    MaxStock = incomingProduct.MaxStock,
                    RestockAmount = incomingProduct.RestockAmount,
                    SortOrder = incomingProduct.SortOrder,
                    LocalVariablesJson = incomingProduct.LocalVariablesJson,
                    AppearanceJson = incomingProduct.AppearanceJson,
                    CurrentStock = incomingProduct.CurrentStock,
                    IsPlayerManaged = incomingProduct.IsPlayerManaged,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
    }
}
