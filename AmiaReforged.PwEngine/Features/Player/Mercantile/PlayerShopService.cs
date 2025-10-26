using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Shops;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

[ServiceBinding(typeof(PlayerShopService))]
public class PlayerShopService
{
    private const string ShopResRef = "player_shop";
    private const int StallDailyFee = 10000;
    private readonly IPlayerShopRepository _shopRepository;
    private readonly WindowDirector _director;

    public PlayerShopService(IPlayerShopRepository shopRepository, WindowDirector director)
    {
        _shopRepository = shopRepository;
        _director = director;
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

            shop.OnUsed += HandlePlayerUse;
        }
    }

    private void HandlePlayerUse(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player)) return;

        bool stallExists = _shopRepository.StallExists((obj.Placeable.Tag, obj.Placeable.Area!.ResRef));
        if (!stallExists)
        {
            player.SendServerMessage("This shop is not properly configured. Screenshot this and send it to the staff",
                ColorConstants.Red);
            return;
        }


        PlayerShopInstance? shop = GetShop(obj.Placeable.Tag, obj.Placeable.Area!.ResRef);

        if (shop == null && stallExists)
        {
            PromptRental(obj.Placeable, player);
            return;
        }
        else
        {
            PresentStore(shop, player);
        }
    }

    private void PromptRental(NwPlaceable objPlaceable, NwPlayer player)
    {
        // Don't let them rent more than one stall per area
        List<PlayerStall> playerStalls = _shopRepository.StallsForPlayer(player.LoginCreature!.ObjectId);
        bool alreadyRentedInArea = playerStalls.Any(s => s.AreaResRef == objPlaceable.Area!.ResRef);

        if (alreadyRentedInArea)
        {
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(player)
                .WithTitle("You already own a stall in this area.")
                .WithMessage(
                    $"Save some room for the other merchants! You can only rent one stall per area.")
                .Open();
        }

        if (player.LoginCreature!.Gold < StallDailyFee)
        {
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(player)
                .WithTitle("You cannot afford to rent this stall.")
                .WithMessage(
                    $"You need at least {StallDailyFee} gold to rent a player stall. This shop costs {StallDailyFee} gold per real life day to rent.")
                .Open();

            return;
        }
        else
        {
            _director.OpenPopupWithReaction(player,
                "Rent Player Stall",
                $"Do you wish to rent this player stall for {StallDailyFee} gold per real life day? Press `X` to cancel, `OK` to confirm.",
                () => { }
            );
        }
    }

    private void PresentStore(PlayerShopInstance? shop, NwPlayer player)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets or loads a player shop instance by its tag and area.
    /// </summary>
    public PlayerShopInstance? GetShop(string tag, string areaResRef)
    {
        // Load from database
        List<PlayerStall> stalls = _shopRepository.ShopsByTag(tag);
        PlayerStall? stall = stalls.FirstOrDefault(s => s.AreaResRef == areaResRef);

        if (stall == null)
            return null;

        PlayerShopInstance shopInstance = new(stall);

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
                    NwItem? gameItem = NwItem.Deserialize(purchased.ItemData);
                    // Deduct gold from buyer
                    if (buyer != null && gameItem != null)
                    {
                        buyer.Gold -= (uint)purchased.Cost;
                        buyer.AcquireItem(gameItem);

                        // N.B: Make sure to only remove the item if the purchase actually succeeded
                        _shopRepository.RemoveProductFromShop(purchased.ShopId, purchased.ItemId.Value);
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
