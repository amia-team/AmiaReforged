using System;
using System.Collections.Generic;
using System.Linq;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed class NpcShop
{
    private readonly List<NpcShopProduct> _products = new();

    public NpcShop(NpcShopDefinition definition, IShopItemBlacklist? blacklist = null)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(definition.Tag))
        {
            throw new ArgumentException("Shop tag must not be empty.", nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            throw new ArgumentException("Shop display name must not be empty.", nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(definition.ShopkeeperTag))
        {
            throw new ArgumentException("Shopkeeper tag must not be empty.", nameof(definition));
        }

        if (definition.Restock == null)
        {
            throw new ArgumentException("Restock configuration must be provided.", nameof(definition));
        }

        if (definition.Products == null || definition.Products.Count == 0)
        {
            throw new ArgumentException("At least one product is required.", nameof(definition));
        }

        Tag = definition.Tag;
        DisplayName = definition.DisplayName;
        Description = definition.Description;
        ShopkeeperTag = definition.ShopkeeperTag;
        RestockPolicy = definition.Restock;

        int filteredCount = 0;

        foreach (NpcShopProductDefinition productDefinition in definition.Products)
        {
            if (blacklist != null && blacklist.IsBlacklisted(productDefinition.ResRef))
            {
                filteredCount++;
                continue;
            }

            NpcShopProduct product = new(
                productDefinition.ResRef,
                productDefinition.Price,
                productDefinition.InitialStock,
                productDefinition.MaxStock,
                productDefinition.RestockAmount);

            _products.Add(product);
        }

        if (_products.Count == 0)
        {
            throw new ArgumentException(
                filteredCount > 0
                    ? "All products were filtered by the shop blacklist."
                    : "At least one product is required.",
                nameof(definition));
        }

        Products = _products;
    }

    public string Tag { get; }
    public string DisplayName { get; }
    public string ShopkeeperTag { get; }
    public string? Description { get; }

    public NpcShopRestockDefinition RestockPolicy { get; }

    public IReadOnlyList<NpcShopProduct> Products { get; }

    public DateTime NextRestockUtc { get; private set; }

    public void SetNextRestock(DateTime utcTime)
    {
        NextRestockUtc = utcTime;
    }

    public void RestockAll()
    {
        foreach (NpcShopProduct product in _products)
        {
            product.Restock();
        }
    }

    public NpcShopProduct? FindProduct(string resRef)
    {
        return _products.FirstOrDefault(p => string.Equals(p.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
    }
}
