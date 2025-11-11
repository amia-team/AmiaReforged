using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

[ServiceBinding(typeof(INpcShopRepository))]
public sealed class NpcShopRepository : INpcShopRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public event EventHandler<NpcShopChangedEventArgs>? ShopChanged;

    private readonly Dictionary<string, NpcShop> _shops = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _shopkeeperToShop = new(StringComparer.OrdinalIgnoreCase);
    private readonly IShopItemBlacklist _blacklist;
    private readonly IShopPersistenceRepository _persistence;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly object _sync = new();

    public NpcShopRepository(IShopItemBlacklist blacklist, IShopPersistenceRepository persistence)
    {
        _blacklist = blacklist;
        _persistence = persistence;
        Reload();
    }

    public void Reload()
    {
        lock (_sync)
        {
            _shops.Clear();
            _shopkeeperToShop.Clear();

            IReadOnlyList<ShopRecord> records = _persistence.GetAllAsync(cancellationToken: CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            foreach (ShopRecord record in records)
            {
                TryCache(record);
            }
        }
    }

    public void Upsert(NpcShopDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        ShopRecord record = BuildRecordFromDefinition(definition, _jsonOptions);
        PersistAndCache(record);
    }

    public void Upsert(ShopRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        PersistAndCache(record);
    }

    public bool TryGet(string shopTag, out NpcShop? shop)
    {
        if (string.IsNullOrWhiteSpace(shopTag))
        {
            shop = null;
            return false;
        }

        lock (_sync)
        {
            return _shops.TryGetValue(shopTag, out shop);
        }
    }

    public bool TryGetByShopkeeper(string shopkeeperTag, out NpcShop? shop)
    {
        shop = null;

        if (string.IsNullOrWhiteSpace(shopkeeperTag))
        {
            return false;
        }

        lock (_sync)
        {
            if (!_shopkeeperToShop.TryGetValue(shopkeeperTag, out string? shopTag))
            {
                return false;
            }

            return _shops.TryGetValue(shopTag, out shop);
        }
    }

    public IReadOnlyCollection<NpcShop> All(ShopKind? kind = null)
    {
        lock (_sync)
        {
            if (!kind.HasValue)
            {
                return _shops.Values.ToList();
            }

            return _shops.Values.Where(s => s.Kind == kind.Value).ToList();
        }
    }

    public bool TryConsumeProduct(string shopTag, string productResRef, int quantity, out ConsignedItemData? consumedItem)
    {
        consumedItem = null;

        if (quantity <= 0)
        {
            return false;
        }

        if (!TryGet(shopTag, out NpcShop? shop) || shop is null)
        {
            return false;
        }

        ConsignedItemData? resultItem = null;
        bool success = false;

        lock (shop.SyncRoot)
        {
            NpcShopProduct? product = shop.FindProduct(productResRef);
            if (product is null)
            {
                return false;
            }

            if (!product.TryConsume(quantity))
            {
                return false;
            }

            try
            {
                bool persisted = _persistence.TryConsumeStockAsync(shop.Id, product.ResRef, quantity, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                if (!persisted)
                {
                    product.ReturnToStock(quantity);
                    return false;
                }

                if (product.IsPlayerManaged)
                {
                    ShopVaultItem? vaultItem = _persistence.TakeVaultItemAsync(shop.Id, product.ResRef, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();

                    if (vaultItem is null)
                    {
                        product.ReturnToStock(quantity);
                        _persistence.ReturnStockAsync(shop.Id, product.ResRef, quantity, CancellationToken.None)
                            .GetAwaiter()
                            .GetResult();
                        return false;
                    }

                    resultItem = new ConsignedItemData(vaultItem.ItemData, vaultItem.Quantity, vaultItem.ItemName, vaultItem.ResRef);
                }

                success = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to persist stock consumption for shop {Tag} product {ResRef}.", shop.Tag, product.ResRef);
                product.ReturnToStock(quantity);
                return false;
            }
        }

        if (!success)
        {
            return false;
        }

        consumedItem = resultItem;
        RaiseShopChanged(shop, NpcShopChangeKind.StockChanged);
        return true;
    }

    public void ReturnProduct(string shopTag, string productResRef, int quantity, ConsignedItemData? consignedItem = null)
    {
        if (quantity <= 0)
        {
            return;
        }

        if (!TryGet(shopTag, out NpcShop? shop) || shop is null)
        {
            return;
        }

        bool updated = false;

        lock (shop.SyncRoot)
        {
            NpcShopProduct? product = shop.FindProduct(productResRef);
            if (product is null)
            {
                return;
            }

            product.ReturnToStock(quantity);
            updated = true;

            try
            {
                _persistence.ReturnStockAsync(shop.Id, product.ResRef, quantity, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                if (consignedItem is not null && product.IsPlayerManaged)
                {
                    _persistence.StoreVaultItemAsync(
                            shop.Id,
                            product.ResRef,
                            consignedItem.ItemName,
                            consignedItem.ItemData,
                            consignedItem.Quantity,
                            CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to persist stock return for shop {Tag} product {ResRef}.", shop.Tag, product.ResRef);
            }
        }

        if (updated)
        {
            RaiseShopChanged(shop, NpcShopChangeKind.StockChanged);
        }
    }

    public bool TryStorePlayerProduct(string shopTag, ShopProductRecord product, ConsignedItemData consignedItem)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(consignedItem);

        if (!TryGet(shopTag, out NpcShop? shop) || shop is null)
        {
            return false;
        }

        bool success = false;

        try
        {
            ShopProductRecord persisted = _persistence.UpsertPlayerProductAsync(
                    shop.Id,
                    product,
                    consignedItem.ItemData,
                    consignedItem.Quantity,
                    consignedItem.ItemName,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            shop.UpsertProduct(persisted);
            success = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to store player-managed product for shop {Tag} resref {ResRef}.", shopTag, product.ResRef);
            success = false;
        }

        if (!success)
        {
            return false;
        }

        RaiseShopChanged(shop, NpcShopChangeKind.ProductsChanged);
        return true;
    }

    public bool TryUpdateNextRestock(string shopTag, DateTime? nextRestockUtc)
    {
        if (!TryGet(shopTag, out NpcShop? shop) || shop is null)
        {
            return false;
        }

        bool success = false;

        lock (shop.SyncRoot)
        {
            DateTime? effective = nextRestockUtc.HasValue && nextRestockUtc.Value == default ? null : nextRestockUtc;
            shop.SetNextRestock(effective);

            try
            {
                _persistence.UpdateNextRestockAsync(shop.Id, effective, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
                success = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to persist restock update for shop {Tag}.", shop.Tag);
                success = false;
            }
        }

        if (!success)
        {
            return false;
        }

        RaiseShopChanged(shop, NpcShopChangeKind.MetadataChanged);
        return true;
    }

    public void ApplyRestock(NpcShop shop, IReadOnlyList<(NpcShopProduct Product, int Added)> restocked)
    {
        ArgumentNullException.ThrowIfNull(shop);
        ArgumentNullException.ThrowIfNull(restocked);

        if (restocked.Count == 0)
        {
            return;
        }

        bool updated = false;

        lock (shop.SyncRoot)
        {
            foreach ((NpcShopProduct product, int added) in restocked)
            {
                if (added <= 0)
                {
                    continue;
                }

                try
                {
                    _persistence.ReturnStockAsync(shop.Id, product.ResRef, added, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                    updated = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to persist restock for shop {Tag} product {ResRef}.", shop.Tag, product.ResRef);
                }
            }
        }

        if (updated)
        {
            RaiseShopChanged(shop, NpcShopChangeKind.StockChanged);
        }
    }

    private void PersistAndCache(ShopRecord record)
    {
        try
        {
            _persistence.UpsertAsync(record, CancellationToken.None).GetAwaiter().GetResult();
            ShopRecord? refreshed = _persistence.GetByTagAsync(record.Tag, cancellationToken: CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (refreshed is null)
            {
                return;
            }

            lock (_sync)
            {
                TryCache(refreshed);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Persisting shop {Tag} failed.", record.Tag);
        }
    }

    private void TryCache(ShopRecord record)
    {
        try
        {
            NpcShop shop = new(record, _blacklist);
            _shops[shop.Tag] = shop;

            if (!string.IsNullOrWhiteSpace(shop.ShopkeeperTag))
            {
                _shopkeeperToShop[shop.ShopkeeperTag] = shop.Tag;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to materialize runtime shop for tag {Tag}.", record.Tag);
        }
    }

    private void RaiseShopChanged(NpcShop shop, NpcShopChangeKind changeKind)
    {
        try
        {
            ShopChanged?.Invoke(this, new NpcShopChangedEventArgs(shop, changeKind));
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "NPC shop change observers threw an exception for shop {Tag}.", shop.Tag);
        }
    }

    private static ShopRecord BuildRecordFromDefinition(NpcShopDefinition definition, JsonSerializerOptions jsonOptions)
    {
        string canonicalDefinition = JsonSerializer.Serialize(definition, jsonOptions);
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalDefinition))).ToLowerInvariant();

        ShopRecord record = new()
        {
            Tag = definition.Tag,
            DisplayName = definition.DisplayName,
            ShopkeeperTag = definition.ShopkeeperTag,
            Description = definition.Description,
            Kind = ShopKind.Npc,
            ManualRestock = false,
            ManualPricing = false,
            RestockMinMinutes = definition.Restock.MinMinutes,
            RestockMaxMinutes = definition.Restock.MaxMinutes,
            DefinitionHash = hash,
            MarkupPercent = Math.Max(0, definition.MarkupPercent),
            AcceptedBaseItemTypesJson = SerializeOrNull(definition.AcceptCategories, jsonOptions),
            VaultBalance = 0,
            Products = new List<ShopProductRecord>()
        };

        if (definition.Products != null)
        {
            int sortOrder = 0;
            foreach (NpcShopProductDefinition product in definition.Products)
            {
                string displayName = product.Name.Trim();
                string? description = string.IsNullOrWhiteSpace(product.Description)
                    ? null
                    : product.Description.Trim();
                int maxStock = Math.Max(0, product.MaxStock);
                int initialStock = Math.Clamp(product.InitialStock, 0, maxStock == 0 ? int.MaxValue : maxStock);

                ShopProductRecord productRecord = new()
                {
                    ResRef = product.ResRef,
                    DisplayName = displayName,
                    Description = description,
                    Price = product.Price,
                    CurrentStock = initialStock,
                    MaxStock = maxStock,
                    RestockAmount = Math.Max(0, product.RestockAmount),
                    SortOrder = sortOrder++,
                    BaseItemType = product.BaseItemType,
                    IsPlayerManaged = false,
                    LocalVariablesJson = SerializeOrNull(product.LocalVariables, jsonOptions),
                    AppearanceJson = SerializeOrNull(product.Appearance, jsonOptions)
                };

                record.Products.Add(productRecord);
            }
        }

        return record;
    }

    private static string? SerializeOrNull(object? value, JsonSerializerOptions options)
    {
        return value == null ? null : JsonSerializer.Serialize(value, options);
    }
}
