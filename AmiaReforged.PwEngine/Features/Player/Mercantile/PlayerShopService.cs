using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Shops;
using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

[ServiceBinding(typeof(PlayerShopService))]
public class PlayerShopService
{
    private const string ShopResRef = "player_shop";

    private readonly IPlayerShopRepository _shopRepository;
    private readonly Dictionary<(string Tag, string AreaResRef), PlayerShopInstance> _loadedShops = new();

    public PlayerShopService(IPlayerShopRepository shopRepository)
    {
        _shopRepository = shopRepository;
        List<NwPlaceable> playerShops =
            NwObject.FindObjectsOfType<NwPlaceable>().Where(p => p.ResRef == ShopResRef).ToList();

        foreach (NwPlaceable shop in playerShops)
        {
            if (shop.Area is null) continue;
            bool exists = shopRepository.StallExists((shop.Tag, shop.Area.ResRef));

            if (exists) continue;

            PlayerStall newStall = new()
            {
                Tag = shop.Tag,
                AreaResRef = shop.Area.ResRef,
                CharacterId = null // No owner yet
            };
            shopRepository.AddStall(newStall);
        }
    }

    /// <summary>
    /// Gets or loads a player shop instance by its tag and area.
    /// </summary>
    public PlayerShopInstance? GetShop(string tag, string areaResRef)
    {
        (string tag, string areaResRef) key = (tag, areaResRef);

        if (_loadedShops.TryGetValue(key, out PlayerShopInstance? cachedShop))
            return cachedShop;

        // Load from database
        List<PlayerStall> stalls = _shopRepository.ShopsByTag(tag);
        PlayerStall? stall = stalls.FirstOrDefault(s => s.AreaResRef == areaResRef);

        if (stall == null)
            return null;

        PlayerShopInstance shopInstance = new(stall);
        _loadedShops[key] = shopInstance;

        return shopInstance;
    }

    /// <summary>
    /// Handles a purchase attempt from a buyer at a specific shop.
    /// Processes domain events to persist changes and create the item.
    /// </summary>
    public PurchaseResult HandlePurchase(NwCreature buyer, string shopTag, string areaResRef, ItemId itemId)
    {
        PlayerShopInstance? shop = GetShop(shopTag, areaResRef);
        if (shop == null)
            return PurchaseResult.Failure("Shop not found.");

        PurchaseResult result = shop.PurchaseItem(buyer, itemId);

        if (result.IsSuccess)
        {
            ProcessDomainEvents(shop, buyer);
        }

        return result;
    }

    /// <summary>
    /// Adds an item to a shop and persists it.
    /// </summary>
    public void AddItemToShop(string shopTag, string areaResRef, NwItem item, int price)
    {
        PlayerShopInstance? shop = GetShop(shopTag, areaResRef);
        if (shop == null)
            return;

        shop.AddItem(item, price);
        ProcessDomainEvents(shop);
    }

    /// <summary>
    /// Removes an item from a shop (e.g., owner reclaiming).
    /// </summary>
    public OperationResult RemoveItemFromShop(string shopTag, string areaResRef, ItemId itemId)
    {
        PlayerShopInstance? shop = GetShop(shopTag, areaResRef);
        if (shop == null)
            return OperationResult.Failure("Shop not found.");

        OperationResult result = shop.RemoveItem(itemId);

        if (result.IsSuccess)
        {
            ProcessDomainEvents(shop);
        }

        return result;
    }

    /// <summary>
    /// Processes domain events from the shop aggregate and persists changes.
    /// </summary>
    private void ProcessDomainEvents(PlayerShopInstance shop, NwCreature? buyer = null)
    {
        foreach (object domainEvent in shop.DomainEvents)
        {
            switch (domainEvent)
            {
                case ItemPurchasedEvent purchased:
                    // Remove from database
                    _shopRepository.RemoveProductFromShop(purchased.ShopId, purchased.ItemId.Value);

                    // Deduct gold from buyer
                    if (buyer != null)
                    {
                        buyer.Gold -= (uint)purchased.Cost;

                        // TODO: Deserialize item from purchased.ItemData and create it on buyer
                        // TODO: Send server message to buyer about purchase
                    }

                    break;

                case ItemAddedToShopEvent added:
                    StallProduct product = new()
                    {
                        Name = added.ItemName,
                        Description = added.ItemDescription,
                        Price = added.Price,
                        ItemData = added.ItemData
                    };
                    _shopRepository.AddProductToShop(added.ShopId, product);
                    break;

                case ItemRemovedFromShopEvent removed:
                    _shopRepository.RemoveProductFromShop(removed.ShopId, removed.ItemId.Value);
                    break;
            }
        }

        shop.ClearDomainEvents();
    }
}

/// <summary>
/// Aggregate root for runtime player shop operations.
/// Manages shop inventory and purchase transactions, raising domain events for persistence.
/// </summary>
public class PlayerShopInstance
{
    public long ShopId { get; }
    public string Tag { get; }
    public string AreaResRef { get; }

    private readonly List<ShopItem> _itemsForSale = new();
    public IReadOnlyList<ShopItem> ItemsForSale => _itemsForSale.AsReadOnly();

    // Domain events queue
    private readonly List<object> _domainEvents = new();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    public PlayerShopInstance(PlayerStall stall)
    {
        ShopId = stall.Id;
        Tag = stall.Tag;
        AreaResRef = stall.AreaResRef;

        // Load items from the stall
        if (stall.Products != null)
        {
            foreach (StallProduct product in stall.Products)
            {
                _itemsForSale.Add(new ShopItem(
                    new ItemId(product.Id),
                    product.Name,
                    product.Description,
                    product.Price,
                    product.ItemData
                ));
            }
        }
    }

    /// <summary>
    /// Attempts to purchase an item from the shop.
    /// </summary>
    /// <returns>Result indicating success or failure with error message.</returns>
    public PurchaseResult PurchaseItem(NwCreature buyer, ItemId itemId)
    {
        ShopItem? item = _itemsForSale.FirstOrDefault(i => i.ItemId == itemId);
        if (item is null)
            return PurchaseResult.Failure("Item not found in shop.");

        if (!CanAfford(buyer, item.Cost))
            return PurchaseResult.Failure($"Cannot afford item. Cost: {item.Cost} gold.");

        // Remove item from runtime inventory
        _itemsForSale.Remove(item);

        // TODO: Get player name from buyer.ControllingPlayer
        string buyerName = "Unknown"; // Placeholder

        // Raise domain event for persistence and item creation
        _domainEvents.Add(new ItemPurchasedEvent(
            ShopId,
            Tag,
            AreaResRef,
            itemId,
            buyerName,
            item.ItemData,
            item.Cost
        ));

        return PurchaseResult.Success(item);
    }

    /// <summary>
    /// Adds an item to the shop inventory.
    /// </summary>
    public void AddItem(NwItem item, int price)
    {
        byte[]? itemData = item.Serialize();

        if (itemData is null)
        {
            if (item.Possessor is not null)
            {
                NWScript.FloatingTextStringOnCreature("Failed to add item to shop.", item.Possessor, NWScript.FALSE);
            }

            return;
        }

        ShopItem shopItem = new(
            new ItemId(0), // Will be set by database
            item.Name,
            item.Description,
            price,
            itemData
        );

        _itemsForSale.Add(shopItem);

        // Raise domain event for persistence
        _domainEvents.Add(new ItemAddedToShopEvent(
            ShopId,
            item.Name,
            item.Description,
            price,
            itemData
        ));
    }

    /// <summary>
    /// Removes an item from the shop without purchasing (e.g., owner removing).
    /// </summary>
    public OperationResult RemoveItem(ItemId itemId)
    {
        ShopItem? item = _itemsForSale.FirstOrDefault(i => i.ItemId == itemId);
        if (item is null)
            return OperationResult.Failure("Item not found in shop.");

        _itemsForSale.Remove(item);

        _domainEvents.Add(new ItemRemovedFromShopEvent(ShopId, itemId));

        return OperationResult.Success();
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private bool CanAfford(NwCreature creature, int amount)
    {
        return creature.Gold >= amount;
    }
}

// Domain Events
public record ItemPurchasedEvent(
    long ShopId,
    string ShopTag,
    string AreaResRef,
    ItemId ItemId,
    string BuyerName,
    byte[] ItemData,
    int Cost
);

public record ItemAddedToShopEvent(
    long ShopId,
    string ItemName,
    string ItemDescription,
    int Price,
    byte[] ItemData
);

public record ItemRemovedFromShopEvent(long ShopId, ItemId ItemId);

// Value Objects and DTOs
public record ItemId(long Value);

public record ShopItem(
    ItemId ItemId,
    string Name,
    string Description,
    int Cost,
    byte[] ItemData
);

public record TransactionReceipt(
    ItemId ItemId,
    string ItemName,
    string BuyerName,
    int Cost,
    DateTime PurchaseDate
);

// Results
public record PurchaseResult(bool IsSuccess, string? ErrorMessage, ShopItem? Item)
{
    public static PurchaseResult Success(ShopItem item) => new(true, null, item);
    public static PurchaseResult Failure(string error) => new(false, error, null);
}

public record OperationResult(bool IsSuccess, string? ErrorMessage)
{
    public static OperationResult Success() => new(true, null);
    public static OperationResult Failure(string error) => new(false, error);
}
