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

    public async Task<bool> TryConsumeStockAsync(
        long shopId,
        long productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return true;
        }

        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        ShopProductRecord? product = await ctx.ShopProducts
            .FirstOrDefaultAsync(p => p.ShopId == shopId && p.Id == productId, cancellationToken);

        if (product is null)
        {
            return false;
        }

        if (product.CurrentStock < quantity)
        {
            return false;
        }

        product.CurrentStock -= quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await ctx.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task ReturnStockAsync(
        long shopId,
        long productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            return;
        }

        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        ShopProductRecord? product = await ctx.ShopProducts
            .FirstOrDefaultAsync(p => p.ShopId == shopId && p.Id == productId, cancellationToken);

        if (product is null)
        {
            return;
        }

        product.CurrentStock += quantity;
        if (product.MaxStock > 0)
        {
            product.CurrentStock = Math.Min(product.CurrentStock, product.MaxStock);
        }

        product.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateNextRestockAsync(
        long shopId,
        DateTime? nextRestockUtc,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        ShopRecord? record = await ctx.Shops.FirstOrDefaultAsync(s => s.Id == shopId, cancellationToken);
        if (record is null)
        {
            return;
        }

        record.NextRestockUtc = nextRestockUtc;
        record.UpdatedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShopProductRecord> UpsertPlayerProductAsync(
        long shopId,
        ShopProductRecord product,
        byte[] itemData,
        int quantity,
        string? itemName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(itemData);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
        }

        if (itemData.Length == 0)
        {
            throw new ArgumentException("Serialized item data must not be empty.", nameof(itemData));
        }

        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        ShopProductRecord? existing = await ctx.ShopProducts.FirstOrDefaultAsync(
            p => p.ShopId == shopId
                 && p.IsPlayerManaged
                 && p.ResRef == product.ResRef
                 && p.LocalVariablesJson == product.LocalVariablesJson
                 && p.AppearanceJson == product.AppearanceJson
                 && p.BaseItemType == product.BaseItemType,
            cancellationToken);

        if (existing is not null)
        {
            existing.CurrentStock += quantity;
            existing.Price = product.Price;
            existing.DisplayName = product.DisplayName;
            existing.Description = product.Description;
            existing.MaxStock = Math.Max(existing.MaxStock, product.MaxStock);
            existing.RestockAmount = product.RestockAmount;
            existing.BaseItemType = product.BaseItemType;
            existing.LocalVariablesJson = product.LocalVariablesJson;
            existing.AppearanceJson = product.AppearanceJson;
            existing.IsPlayerManaged = true;
            existing.SortOrder = product.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await ctx.SaveChangesAsync(cancellationToken);
            return existing;
        }

        else
        {
            product.ShopId = shopId;
            product.CurrentStock = quantity;
            product.IsPlayerManaged = true;
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            ctx.ShopProducts.Add(product);
            existing = product;
        }

        ShopVaultItem vaultItem = new()
        {
            ShopId = shopId,
            ItemData = itemData,
            ItemName = itemName,
            ResRef = product.ResRef,
            Quantity = quantity,
            StoredAtUtc = DateTime.UtcNow
        };

        ctx.ShopVaultItems.Add(vaultItem);
        await ctx.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task StoreVaultItemAsync(
        long shopId,
        string resRef,
        string? itemName,
        byte[] itemData,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be positive.");
        }

        ArgumentNullException.ThrowIfNull(itemData);

        if (itemData.Length == 0)
        {
            throw new ArgumentException("Serialized item data must not be empty.", nameof(itemData));
        }

        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        ShopVaultItem vaultItem = new()
        {
            ShopId = shopId,
            ItemData = itemData,
            ItemName = itemName,
            ResRef = resRef,
            Quantity = quantity,
            StoredAtUtc = DateTime.UtcNow
        };

        ctx.ShopVaultItems.Add(vaultItem);
        await ctx.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShopVaultItem?> TakeVaultItemAsync(
        long shopId,
        string resRef,
        CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = contextFactory.CreateDbContext();

        ShopVaultItem? item = await ctx.ShopVaultItems
            .Where(v => v.ShopId == shopId && v.ResRef == resRef)
            .OrderBy(v => v.StoredAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
        {
            return null;
        }

        ctx.ShopVaultItems.Remove(item);
        await ctx.SaveChangesAsync(cancellationToken);
        return item;
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
        existing.MarkupPercent = incoming.MarkupPercent;
        existing.AcceptedBaseItemTypesJson = incoming.AcceptedBaseItemTypesJson;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    private static void SyncProducts(ShopRecord existing, IReadOnlyCollection<ShopProductRecord> incoming)
    {
        // Use composite key to match products: ResRef + LocalVariablesJson + AppearanceJson + BaseItemType
        // This allows multiple templates with the same base ResRef to coexist
        Dictionary<string, ShopProductRecord> existingByCompositeKey = existing.Products
            .ToDictionary(p => BuildProductCompositeKey(p.ResRef, p.LocalVariablesJson, p.AppearanceJson, p.BaseItemType),
                StringComparer.OrdinalIgnoreCase);

        foreach (ShopProductRecord incomingProduct in incoming)
        {
            string compositeKey = BuildProductCompositeKey(
                incomingProduct.ResRef,
                incomingProduct.LocalVariablesJson,
                incomingProduct.AppearanceJson,
                incomingProduct.BaseItemType);

            if (existingByCompositeKey.TryGetValue(compositeKey, out ShopProductRecord? record))
            {
                record.DisplayName = incomingProduct.DisplayName;
                record.Description = incomingProduct.Description;
                record.Price = incomingProduct.Price;
                record.MaxStock = incomingProduct.MaxStock;
                record.RestockAmount = incomingProduct.RestockAmount;
                record.SortOrder = incomingProduct.SortOrder;
                record.LocalVariablesJson = incomingProduct.LocalVariablesJson;
                record.AppearanceJson = incomingProduct.AppearanceJson;
                record.BaseItemType = incomingProduct.BaseItemType;
                record.IsPlayerManaged = incomingProduct.IsPlayerManaged;
                record.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                existing.Products.Add(new ShopProductRecord
                {
                    ResRef = incomingProduct.ResRef,
                    DisplayName = incomingProduct.DisplayName,
                    Description = incomingProduct.Description,
                    Price = incomingProduct.Price,
                    MaxStock = incomingProduct.MaxStock,
                    RestockAmount = incomingProduct.RestockAmount,
                    SortOrder = incomingProduct.SortOrder,
                    LocalVariablesJson = incomingProduct.LocalVariablesJson,
                    AppearanceJson = incomingProduct.AppearanceJson,
                    BaseItemType = incomingProduct.BaseItemType,
                    CurrentStock = incomingProduct.CurrentStock,
                    IsPlayerManaged = incomingProduct.IsPlayerManaged,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
    }

    private static string BuildProductCompositeKey(string resRef, string? localsJson, string? appearanceJson, int? baseItemType)
    {
        // Create a composite key that uniquely identifies a product template
        return $"{resRef}|{localsJson ?? ""}|{appearanceJson ?? ""}|{baseItemType?.ToString() ?? ""}";
    }
}
