using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

/// <summary>
/// Aggregate root for runtime player shop operations.
/// Manages shop inventory and purchase transactions, raising domain events for persistence.
/// </summary>
public class PlayerShopInstance
{
    public long ShopId { get; }
    public string Tag { get; }
    public string AreaResRef { get; }

    public delegate void OnItemPurchasedHandler(ItemId itemId, string buyerName);

    private readonly List<ShopItem> _itemsForSale = [];
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


        string buyerName = buyer.Name;

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

    public void UpdateShop(List<ShopItem> updatedItems)
    {
        _itemsForSale.Clear();
        _itemsForSale.AddRange(updatedItems);
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
