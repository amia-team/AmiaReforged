using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public sealed class NpcShop
{
    private static readonly JsonSerializerOptions LocalVariableJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions AppearanceJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<long, NpcShopProduct> _productsById = new();
    private readonly Dictionary<string, List<NpcShopProduct>> _productsByResref = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<NpcShopProduct>> _productsByBaseItemType = new();
    private readonly HashSet<int> _acceptedBaseItemTypes = [];
    private readonly List<NpcShopProduct> _products = [];
    private readonly IItemDefinitionRepository? _itemDefinitions;

    public NpcShop(ShopRecord record, IShopItemBlacklist? blacklist = null, IItemDefinitionRepository? itemDefinitions = null)
    {
        _itemDefinitions = itemDefinitions;
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
        ShopkeeperTag = record.ShopkeeperTag;
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
        MarkupPercent = Math.Max(0, record.MarkupPercent);

        if (!string.IsNullOrWhiteSpace(record.AcceptedBaseItemTypesJson))
        {
            try
            {
                int[]? categories = JsonSerializer.Deserialize<int[]>(record.AcceptedBaseItemTypesJson);
                if (categories is { Length: > 0 })
                {
                    foreach (int category in categories)
                    {
                        if (category >= 0)
                        {
                            _acceptedBaseItemTypes.Add(category);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignored; malformed configuration falls back to no accepted categories.
            }
        }

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

                var blueprint = _itemDefinitions?.GetByTag(productRecord.ResRef);
                IReadOnlyList<NpcShopLocalVariable> locals = BuildLocalVariables(productRecord.LocalVariablesJson);
                // Merge blueprint locals if available
                if (blueprint?.LocalVariables is { Count: > 0 })
                {
                    Dictionary<string, NpcShopLocalVariable> merged = new(StringComparer.OrdinalIgnoreCase);
                    foreach (var bpLocal in blueprint.LocalVariables)
                    {
                        try { merged[bpLocal.Name] = NpcShopLocalVariable.FromDefinition(bpLocal); } catch { }
                    }
                    foreach (var shopLocal in locals)
                    {
                        merged[shopLocal.Name] = shopLocal; // override
                    }
                    locals = merged.Values.ToList();
                }
                SimpleModelAppearance? appearance = BuildAppearance(productRecord.AppearanceJson);
                if (appearance == null && blueprint?.Appearance is not null)
                {
                    appearance = new SimpleModelAppearance(blueprint.Appearance.ModelType, blueprint.Appearance.SimpleModelNumber);
                }
                int? baseItemType2 = productRecord.BaseItemType;
                if ((baseItemType2 is null or < 0) && blueprint?.BaseItemType > 0)
                {
                    baseItemType2 = blueprint.BaseItemType;
                }
                string displayName = string.IsNullOrWhiteSpace(productRecord.DisplayName)
                    ? (blueprint?.Name ?? productRecord.ResRef)
                    : productRecord.DisplayName;
                string? description = string.IsNullOrWhiteSpace(productRecord.Description)
                    ? blueprint?.Description
                    : productRecord.Description;

                NpcShopProduct product = new(
                    productRecord.Id,
                    productRecord.ResRef,
                    displayName,
                    description,
                    productRecord.Price,
                    productRecord.CurrentStock,
                    productRecord.MaxStock,
                    productRecord.RestockAmount,
                    productRecord.IsPlayerManaged,
                    productRecord.SortOrder,
                    baseItemType2,
                    locals,
                    appearance);

                _products.Add(product);
                _productsById[product.Id] = product;

                // ResRef now maps to a list to support multiple templates with the same base ResRef
                if (!_productsByResref.TryGetValue(product.ResRef, out List<NpcShopProduct>? resrefList))
                {
                    resrefList = [];
                    _productsByResref[product.ResRef] = resrefList;
                }
                resrefList.Add(product);

                if (productRecord.BaseItemType is int baseItemType)
                {
                    if (!_productsByBaseItemType.TryGetValue(baseItemType, out List<NpcShopProduct>? list))
                    {
                        list = [];
                        _productsByBaseItemType[baseItemType] = list;
                    }

                    list.Add(product);
                }
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
    public int MarkupPercent { get; }
    public IReadOnlyCollection<int> AcceptedBaseItemTypes => _acceptedBaseItemTypes;

    public bool IsPlayerManagedShop => Kind == ShopKind.Player;

    public void SetNextRestock(DateTime? utcTime)
    {
        NextRestockUtc = utcTime ?? default;
    }

    public IReadOnlyList<(NpcShopProduct Product, int Added)> RestockAll()
    {
        List<(NpcShopProduct Product, int Added)> restocked = [];

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

        // Return first product with matching ResRef (for backward compatibility)
        return _productsByResref.TryGetValue(resRef, out List<NpcShopProduct>? products) && products.Count > 0
            ? products[0]
            : null;
    }

    public NpcShopProduct? FindProductById(long productId)
    {
        return _productsById.TryGetValue(productId, out NpcShopProduct? product) ? product : null;
    }

    public IReadOnlyList<NpcShopProduct> FindProductsByResRef(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef))
        {
            return Array.Empty<NpcShopProduct>();
        }

        return _productsByResref.TryGetValue(resRef, out List<NpcShopProduct>? products)
            ? products
            : Array.Empty<NpcShopProduct>();
    }

    public IReadOnlyList<NpcShopProduct> FindProductsByBaseItemType(int baseItemType)
    {
        return _productsByBaseItemType.TryGetValue(baseItemType, out List<NpcShopProduct>? products)
            ? products
            : Array.Empty<NpcShopProduct>();
    }

    public bool AcceptsBaseItemType(int baseItemType)
    {
        return _acceptedBaseItemTypes.Contains(baseItemType);
    }

    public NpcShopProduct UpsertProduct(ShopProductRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (SyncRoot)
        {
            // Check by ID first for existing products
            if (_productsById.TryGetValue(record.Id, out NpcShopProduct? existing))
            {
                existing.SetCurrentStock(record.CurrentStock);
                return existing;
            }

            var blueprint = _itemDefinitions?.GetByTag(record.ResRef);
            IReadOnlyList<NpcShopLocalVariable> locals = BuildLocalVariables(record.LocalVariablesJson);
            if (blueprint?.LocalVariables is { Count: > 0 })
            {
                Dictionary<string, NpcShopLocalVariable> merged = new(StringComparer.OrdinalIgnoreCase);
                foreach (var bpLocal in blueprint.LocalVariables)
                {
                    try { merged[bpLocal.Name] = NpcShopLocalVariable.FromDefinition(bpLocal); } catch { }
                }
                foreach (var shopLocal in locals)
                {
                    merged[shopLocal.Name] = shopLocal;
                }
                locals = merged.Values.ToList();
            }
            SimpleModelAppearance? appearance = BuildAppearance(record.AppearanceJson);
            if (appearance == null && blueprint?.Appearance is not null)
            {
                appearance = new SimpleModelAppearance(blueprint.Appearance.ModelType, blueprint.Appearance.SimpleModelNumber);
            }
            int? baseType = record.BaseItemType;
            if ((baseType is null or < 0) && blueprint?.BaseItemType > 0)
            {
                baseType = blueprint.BaseItemType;
            }
            string displayName = string.IsNullOrWhiteSpace(record.DisplayName)
                ? (blueprint?.Name ?? record.ResRef)
                : record.DisplayName;
            string? description = string.IsNullOrWhiteSpace(record.Description)
                ? blueprint?.Description
                : record.Description;

            NpcShopProduct product = new(
                record.Id,
                record.ResRef,
                displayName,
                description,
                record.Price,
                record.CurrentStock,
                record.MaxStock,
                record.RestockAmount,
                record.IsPlayerManaged,
                record.SortOrder,
                baseType,
                locals,
                appearance);

            _products.Add(product);
            _productsById[product.Id] = product;

            // Add to ResRef list
            if (!_productsByResref.TryGetValue(product.ResRef, out List<NpcShopProduct>? resrefList))
            {
                resrefList = [];
                _productsByResref[product.ResRef] = resrefList;
            }
            resrefList.Add(product);

            if (record.BaseItemType is int baseItemType)
            {
                if (!_productsByBaseItemType.TryGetValue(baseItemType, out List<NpcShopProduct>? list))
                {
                    list = [];
                    _productsByBaseItemType[baseItemType] = list;
                }

                list.Add(product);
            }

            _products.Sort(static (left, right) => left.SortOrder.CompareTo(right.SortOrder));
            return product;
        }
    }

    public void SetVaultBalance(int amount)
    {
        VaultBalance = Math.Max(0, amount);
    }

    private static IReadOnlyList<NpcShopLocalVariable> BuildLocalVariables(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            JsonLocalVariableDefinition[]? definitions = JsonSerializer.Deserialize<JsonLocalVariableDefinition[]>(json, LocalVariableJsonOptions);
            if (definitions is null || definitions.Length == 0)
            {
                return [];
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
            return [];
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
            SimpleModelAppearanceDefinition? definition = JsonSerializer.Deserialize<SimpleModelAppearanceDefinition>(json, AppearanceJsonOptions);
            return definition == null ? null : new SimpleModelAppearance(definition.ModelType, definition.SimpleModelNumber);
        }
        catch
        {
            return null;
        }
    }
}
