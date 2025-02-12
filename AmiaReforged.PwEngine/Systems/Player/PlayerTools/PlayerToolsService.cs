using AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools;

[ServiceBinding(typeof(PlayerToolsService))]
public class PlayerToolsService
{
    private readonly WindowDirector _windowManager;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string EntryAreaTag = "welcometotheeete";
    private const int PlayerToolsFeatId = 1337;
    
    public PlayerToolsService(WindowDirector windowManager)
    {
        _windowManager = windowManager;
        NwArea? entryArea = NwModule.Instance.Areas.FirstOrDefault(t => t.Tag == EntryAreaTag);
        
        if (entryArea == null)
        {
            Log.Error("Entry area not found.");
            return;
        }
        
        entryArea.OnEnter += AddPlayerToolsFeat;

        NwModule.Instance.OnUseFeat += OnUsePlayerTools;
    }

    private void OnUsePlayerTools(OnUseFeat obj)
    {
        if (obj.Feat.Id != PlayerToolsFeatId) return;

        if (!obj.Creature.IsPlayerControlled(out NwPlayer? player)) return;
        
        if(_windowManager.IsWindowOpen(player, typeof(PlayerToolsWindowPresenter)))
        {
            player.FloatingTextString("Player Tools window is already open.", false);
            return;
        }
        
        InjectionService? injector = Anvil.AnvilCore.GetService<InjectionService>();
        if (injector is null)
        {
            player.FloatingTextString("Failed to load the player tools due to missing DI container. Screenshot this and report it as a bug.", false);
            return;
        }
        PlayerToolsWindowView window = new(player);
        PlayerToolsWindowPresenter presenter = window.ToolPresenter;

        injector.Inject(presenter);
        _windowManager.OpenWindow(presenter);
    }

    private void AddPlayerToolsFeat(AreaEvents.OnEnter obj)
    {
        if(!obj.EnteringObject.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature? character = player.LoginCreature;

        if (character is null)
        {
            Log.Error($"{player.PlayerName}'s Character not found.");
            return;
        }
        
        if(character.Feats.Any(f => f.Id == PlayerToolsFeatId)) return;

        NwFeat? playerTools = NwFeat.FromFeatId(PlayerToolsFeatId);

        if (playerTools is null)
        {
            Log.Error("PlayerTools feat not found.");
            return;
        }
        
        player.FloatingTextString("Adding PlayerTools feat.", false);
        CreaturePlugin.AddFeatByLevel(character, PlayerToolsFeatId, 1);
    }
}