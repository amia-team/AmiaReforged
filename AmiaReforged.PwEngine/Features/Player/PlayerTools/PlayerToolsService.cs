using AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools;

[ServiceBinding(typeof(PlayerToolsService))]
public class PlayerToolsService
{
    private const int PlayerToolsFeatId = 1337;
    private const int WeaponFinesseFeatId = 42;
    private const int RapidReloadFeatId = 411;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowManager;

    public PlayerToolsService(WindowDirector windowManager)
    {
        _windowManager = windowManager;
        NwArea entryArea = NwModule.Instance.StartingLocation.Area;

        entryArea.OnEnter += AddPlayerToolsFeat;
        entryArea.OnEnter += AddWeaponFinesseFeat;
        entryArea.OnEnter += AddRapidReloadFeat;

        NwModule.Instance.OnUseFeat += OnUsePlayerTools;
    }

    private void OnUsePlayerTools(OnUseFeat obj)
    {
        if (obj.Feat.Id != PlayerToolsFeatId) return;

        if (!obj.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowManager.IsWindowOpen(player, typeof(PlayerToolsWindowPresenter)))
        {
            player.FloatingTextString(message: "Player Tools window is already open.", false);
            return;
        }

        InjectionService? injector = AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            player.FloatingTextString(
                message:
                "Failed to load the player tools due to missing DI container. Screenshot this and report it as a bug.",
                false);
            return;
        }

        PlayerToolsWindowView window = new(player);
        PlayerToolsWindowPresenter presenter = window.Presenter;

        injector.Inject(presenter);
        _windowManager.OpenWindow(presenter);
    }

    private void AddPlayerToolsFeat(AreaEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;

        if (character is null)
        {
            Log.Error($"{player.PlayerName}'s Character not found.");
            return;
        }

        if (character.Feats.Any(f => f.Id == PlayerToolsFeatId)) return;

        NwFeat? playerTools = NwFeat.FromFeatId(PlayerToolsFeatId);

        if (playerTools is null)
        {
            Log.Error(message: "PlayerTools feat not found.");
            return;
        }

        player.FloatingTextString(message: "Adding PlayerTools feat.", false);
        CreaturePlugin.AddFeatByLevel(character, PlayerToolsFeatId, 1);
    }
    private void AddWeaponFinesseFeat(AreaEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;

        if (character is null)
        {
            Log.Error($"{player.PlayerName}'s Character not found.");
            return;
        }

        if (character.Feats.Any(f => f.Id == WeaponFinesseFeatId)) return;

        NwFeat? weaponFinesse = NwFeat.FromFeatId(WeaponFinesseFeatId);

        if (weaponFinesse is null)
        {
            Log.Error(message: "Weapon Finesse feat not found.");
            return;
        }

        player.FloatingTextString(message: "Adding Weapon Finesse feat.", false);
        CreaturePlugin.AddFeatByLevel(character, WeaponFinesseFeatId, 1);
    }

    private void AddRapidReloadFeat(AreaEvents.OnEnter obj)
    {
        if (!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;

        if (character is null)
        {
            Log.Error($"{player.PlayerName}'s Character not found.");
            return;
        }

        if (character.Feats.Any(f => f.Id == RapidReloadFeatId)) return;

        NwFeat? rapidReload = NwFeat.FromFeatId(RapidReloadFeatId);

        if (rapidReload is null)
        {
            Log.Error(message: "Rapid Reload feat not found.");
            return;
        }

        player.FloatingTextString(message: "Adding Rapid Reload feat.", false);
        CreaturePlugin.AddFeatByLevel(character, RapidReloadFeatId, 1);
    }
}
