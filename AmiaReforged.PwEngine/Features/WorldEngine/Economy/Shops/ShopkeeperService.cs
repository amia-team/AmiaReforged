using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Sanitization;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

[ServiceBinding(typeof(ShopkeeperService))]
public sealed class ShopkeeperService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string ShopWindowFlagPrefix = "engine_npc_shop_window_";

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

        NwPlayer? player = ResolvePlayer(eventData, npc, shop);
        if (player is null)
        {
            return;
        }

        OpenShopWindow(player, shop, npc);
    }

    private NwPlayer? ResolvePlayer(CreatureEvents.OnConversation eventData, NwCreature npc, NpcShop shop)
    {
        string flagKey = BuildShopWindowFlagKey(shop.Tag);

        NwPlayer? speaker = eventData.PlayerSpeaker;
        if (IsEligibleSpeaker(speaker, flagKey))
        {
            return speaker;
        }

        NwPlayer? fallback = speaker;

        foreach (NwCreature candidate in npc.GetNearestCreatures(CreatureTypeFilter.Perception(PerceptionType.Seen)))
        {
            if (candidate.IsLoginPlayerCharacter(out NwPlayer? player))
            {
                fallback ??= player;

                NwCreature? windowCreature = ResolveWindowCreature(player);
                if (windowCreature is null)
                {
                    return player;
                }

                if (!HasActiveShopWindow(windowCreature, flagKey))
                {
                    return player;
                }
            }
        }

        return fallback;
    }

    private void OpenShopWindow(NwPlayer player, NpcShop shop, NwCreature shopkeeper)
    {
        if (!player.IsValid)
        {
            return;
        }

        string flagKey = BuildShopWindowFlagKey(shop.Tag);
        NwCreature? occupant = ResolveWindowCreature(player);

        if (occupant is { IsValid: true } && HasActiveShopWindow(occupant, flagKey))
        {
            ClearShopWindowFlag(occupant, flagKey);
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

        IScryPresenter.PresenterClosedEventHandler? closingHandler = null;

        if (occupant is { IsValid: true })
        {
            closingHandler = (_, _) =>
            {
                if (occupant.IsValid)
                {
                    ClearShopWindowFlag(occupant, flagKey);
                }

                view.Presenter.Closing -= closingHandler;
            };

            view.Presenter.Closing += closingHandler;
        }

        _windowDirector.OpenWindow(view.Presenter);

        if (occupant is { IsValid: true } && view.Presenter.Token().Token != 0)
        {
            SetShopWindowFlag(occupant, flagKey);
        }
        else if (closingHandler is not null)
        {
            view.Presenter.Closing -= closingHandler;
        }

        string message = string.IsNullOrWhiteSpace(displayName)
            ? "Browsing merchant wares."
            : $"Browsing {displayName}.";
        player.SendServerMessage(message, ColorConstants.Cyan);
    }

    private static string BuildShopWindowFlagKey(string shopTag)
    {
        return LocalVariableKeyUtility.BuildKey(ShopWindowFlagPrefix, shopTag);
    }

    private static NwCreature? ResolveWindowCreature(NwPlayer player)
    {
        return player.ControlledCreature ?? player.LoginCreature;
    }

    private static bool IsEligibleSpeaker(NwPlayer? player, string flagKey)
    {
        if (player is null || !player.IsValid)
        {
            return false;
        }

        NwCreature? creature = ResolveWindowCreature(player);
        if (creature is null)
        {
            return true;
        }

        return !HasActiveShopWindow(creature, flagKey);
    }

    private static bool HasActiveShopWindow(NwCreature creature, string flagKey)
    {
        LocalVariableInt marker = creature.GetObjectVariable<LocalVariableInt>(flagKey);
        return marker.HasValue && marker.Value != 0;
    }

    private static void SetShopWindowFlag(NwCreature creature, string flagKey)
    {
        creature.GetObjectVariable<LocalVariableInt>(flagKey).Value = 1;
    }

    private static void ClearShopWindowFlag(NwCreature creature, string flagKey)
    {
        LocalVariableInt marker = creature.GetObjectVariable<LocalVariableInt>(flagKey);
        if (marker.HasValue)
        {
            marker.Delete();
        }
    }
}
