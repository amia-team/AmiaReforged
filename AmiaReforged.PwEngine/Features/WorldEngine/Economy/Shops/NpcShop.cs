using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed class NpcShop
{
    private readonly Dictionary<string, NpcShopProduct> _productsByResref = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<NpcShopProduct> _products = new();

    public NpcShop(ShopRecord record, IShopItemBlacklist? blacklist = null)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.Tag))
        {
            throw new ArgumentException("Shop tag must not be empty.", nameof(record));
        }

        if (string.IsNullOrWhiteSpace(record.DisplayName))
        {
            throw new ArgumentException("Shop display name must not be empty.", nameof(record));
        }

        Id = record.Id;
        Tag = record.Tag;
        DisplayName = record.DisplayName;
        ShopkeeperTag = record.ShopkeeperTag ?? string.Empty;
        Description = record.Description;
        Kind = record.Kind;
        ManualRestock = record.ManualRestock;
        ManualPricing = record.ManualPricing;
        OwnerAccountId = record.OwnerAccountId;
        OwnerCharacterId = record.OwnerCharacterId;
        OwnerDisplayName = record.OwnerDisplayName;
        RestockPolicy = new NpcShopRestockDefinition(
            Math.Max(record.RestockMinMinutes, 0),
            Math.Max(record.RestockMaxMinutes, 0));
        NextRestockUtc = record.NextRestockUtc ?? default;
        VaultBalance = record.VaultBalance;
        DefinitionHash = record.DefinitionHash;

        if (record.Products is { Count: > 0 })
        {
            foreach (ShopProductRecord productRecord in record.Products
                         .OrderBy(p => p.SortOrder)
                         .ThenBy(p => p.ResRef, StringComparer.OrdinalIgnoreCase))
            {
                if (blacklist != null && blacklist.IsBlacklisted(productRecord.ResRef))
                {
                    continue;
                }

                IReadOnlyList<NpcShopLocalVariable> locals = BuildLocalVariables(productRecord.LocalVariablesJson);
                SimpleModelAppearance? appearance = BuildAppearance(productRecord.AppearanceJson);

                NpcShopProduct product = new(
                    productRecord.Id,
                    productRecord.ResRef,
                    productRecord.Price,
                    productRecord.CurrentStock,
                    productRecord.MaxStock,
                    productRecord.RestockAmount,
                    productRecord.IsPlayerManaged,
                    productRecord.SortOrder,
                    locals,
                    appearance);

                _products.Add(product);
                _productsByResref[product.ResRef] = product;
            }
        }

        Products = _products;
    }

    public object SyncRoot { get; } = new();

    public long Id { get; }
    public string Tag { get; }
    public string DisplayName { get; }
    public string ShopkeeperTag { get; }
    public string? Description { get; }
    public ShopKind Kind { get; }
    public bool ManualRestock { get; }
    public bool ManualPricing { get; }
    public Guid? OwnerAccountId { get; }
    public Guid? OwnerCharacterId { get; }
    public string? OwnerDisplayName { get; }
    public NpcShopRestockDefinition RestockPolicy { get; }
    public IReadOnlyList<NpcShopProduct> Products { get; }
    public DateTime NextRestockUtc { get; private set; }
    public int VaultBalance { get; private set; }
    public string? DefinitionHash { get; }

    public bool IsPlayerManagedShop => Kind == ShopKind.Player;

    public void SetNextRestock(DateTime? utcTime)
    {
        NextRestockUtc = utcTime ?? default;
    }

    public IReadOnlyList<(NpcShopProduct Product, int Added)> RestockAll()
    {
        List<(NpcShopProduct Product, int Added)> restocked = new();

        if (ManualRestock)
        {
            return restocked;
        }

        foreach (NpcShopProduct product in _products)
        {
            int added = product.Restock();
            if (added > 0)
            {
                restocked.Add((product, added));
            }
        }

        return restocked;
    }

    public NpcShopProduct? FindProduct(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef))
        {
            return null;
        }

        return _productsByResref.TryGetValue(resRef, out NpcShopProduct? product) ? product : null;
    }

    public void SetVaultBalance(int amount)
    {
        VaultBalance = Math.Max(0, amount);
    }

    private static IReadOnlyList<NpcShopLocalVariable> BuildLocalVariables(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<NpcShopLocalVariable>();
        }

        try
        {
            JsonLocalVariableDefinition[]? definitions = JsonSerializer.Deserialize<JsonLocalVariableDefinition[]>(json);
            if (definitions is null || definitions.Length == 0)
            {
                return Array.Empty<NpcShopLocalVariable>();
            }

            List<NpcShopLocalVariable> locals = new(definitions.Length);
            foreach (JsonLocalVariableDefinition definition in definitions)
            {
                locals.Add(NpcShopLocalVariable.FromDefinition(definition));
            }

            return locals;
        }
        catch (JsonException)
        {
            return Array.Empty<NpcShopLocalVariable>();
        }
    }

    private static SimpleModelAppearance? BuildAppearance(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            SimpleModelAppearanceDefinition? definition = JsonSerializer.Deserialize<SimpleModelAppearanceDefinition>(json);
            return definition == null ? null : new SimpleModelAppearance(definition.ModelType, definition.SimpleModelNumber);
        }
        catch
        {
            return null;
        }
    }
}
