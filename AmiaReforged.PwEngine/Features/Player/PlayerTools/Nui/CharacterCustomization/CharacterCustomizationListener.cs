using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

/// <summary>
/// Event listener for opening Character Customization window from NPC conversation
/// </summary>
[ServiceBinding(typeof(CharacterCustomizationListener))]
public class CharacterCustomizationListener
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WindowDirector _windowDirector;

    public CharacterCustomizationListener(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
        Log.Info(message: "CharacterCustomizationListener initialized.");
    }

    [ScriptHandler("nui_craft")]
    public void OnConversation(CallInfo callInfo)
    {
        Log.Info("Player crafting script handler triggered");

        // Get the PC who is speaking in the conversation
        uint pcObject = NWScript.GetLastSpeaker();
        NwCreature? creature = pcObject.ToNwObject<NwCreature>();

        if (creature == null || !creature.IsValid)
        {
            Log.Warn("Creature is null or invalid");
            return;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player == null)
        {
            Log.Warn("Player is null");
            return;
        }

        Log.Info($"Opening Character Customization for player: {player.PlayerName}");

        // Open the Character Customization window through WindowDirector
        CharacterCustomizationView view = new CharacterCustomizationView(player);
        _windowDirector.OpenWindow(view.Presenter);

        player.SendServerMessage("Opening Character Customization...", ColorConstants.Cyan);
    }
}

