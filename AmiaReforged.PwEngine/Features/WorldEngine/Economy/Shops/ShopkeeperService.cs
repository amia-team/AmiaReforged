using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Nui;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

[ServiceBinding(typeof(ShopkeeperService))]
public sealed class ShopkeeperService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly INpcShopRepository _shops;
    private readonly WindowDirector _windowDirector;
    private readonly ShopLocationResolver _shopLocations;
    private readonly HashSet<NwCreature> _registeredCreatures = new();

    public ShopkeeperService(INpcShopRepository shops, WindowDirector windowDirector,
        ShopLocationResolver shopLocations)
    {
        _shops = shops;
        _windowDirector = windowDirector;
        _shopLocations = shopLocations;

        RegisterExistingShopkeepers();
        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void RegisterExistingShopkeepers()
    {
        foreach (NpcShop shop in _shops.All())
        {
            TryRegisterShopkeeper(shop.ShopkeeperTag);
        }
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad eventData)
    {
        RegisterExistingShopkeepers();
    }

    private void TryRegisterShopkeeper(string shopkeeperTag)
    {
        if (string.IsNullOrWhiteSpace(shopkeeperTag))
        {
            return;
        }

        List<NwCreature> candidates = NwObject.FindObjectsWithTag<NwCreature>(shopkeeperTag).ToList();
        if (candidates.Count == 0)
        {
            Log.Debug("No active NPC instances found for shopkeeper tag {Tag} during initialization.", shopkeeperTag);
            return;
        }

        foreach (NwCreature npc in candidates)
        {
            RegisterCreature(npc);
        }
    }

    private void RegisterCreature(NwCreature npc)
    {
        if (_registeredCreatures.Contains(npc))
        {
            return;
        }

        _registeredCreatures.Add(npc);
        npc.OnConversation += HandleShopConversation;
        Log.Info("Shopkeeper registration complete for {NpcName} ({Tag}).", npc.Name, npc.Tag ?? "<no-tag>");
    }

    private void HandleShopConversation(CreatureEvents.OnConversation eventData)
    {
        NwCreature? npc = eventData.Creature;
        if (npc is null)
        {
            return;
        }

        string? tag = npc.Tag;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        if (!_shops.TryGetByShopkeeper(tag, out NpcShop? shop) || shop is null)
        {
            Log.Warn("Conversation triggered for unregistered shopkeeper tag {Tag}.", tag);
            return;
        }

        NwPlayer? player = ResolvePlayer(npc);
        if (player is null)
        {
            return;
        }

        OpenShopWindow(player, shop, npc);
    }

    private NwPlayer? ResolvePlayer(NwCreature npc)
    {
        foreach (NwCreature candidate in npc.GetNearestCreatures(CreatureTypeFilter.Perception(PerceptionType.Seen)))
        {
            if (candidate.IsLoginPlayerCharacter(out NwPlayer? player))
            {
                return player;
            }
        }

        return null;
    }

    private void OpenShopWindow(NwPlayer player, NpcShop shop, NwCreature shopkeeper)
    {
        if (!player.IsValid)
        {
            return;
        }

        _windowDirector.CloseWindow(player, typeof(ShopWindowPresenter));

        string displayName = shop.DisplayName;
        if (_shopLocations.TryResolve(shop, shopkeeper, out ShopLocationMetadata location))
        {
            if (!string.IsNullOrWhiteSpace(location.ShopDisplayName))
            {
                displayName = location.ShopDisplayName;
            }

            Log.Debug("Shop '{ShopTag}' resolved to settlement '{Settlement}' in region '{Region}' via POI '{PoiTag}'.",
                location.ShopTag,
                location.Settlement.Value,
                location.RegionTag.Value,
                location.PoiTag ?? "<unknown>");
        }
        else
        {
            string areaResRef = shopkeeper.Area?.ResRef ?? "<unknown>";
            Log.Warn("Shop '{ShopTag}' at area '{AreaResRef}' lacks a matching shop POI in region data.",
                shop.Tag, areaResRef);
        }

        ShopWindowView view = new(player, shop);
        _windowDirector.OpenWindow(view.Presenter);

        string message = string.IsNullOrWhiteSpace(displayName)
            ? "Browsing merchant wares."
            : $"Browsing {displayName}.";
        player.SendServerMessage(message, ColorConstants.Cyan);
    }
}
