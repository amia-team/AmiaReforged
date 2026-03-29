using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// Service for managing the Prestige Divine Spellbook UI window.
/// Provides the interface for Rangers/Paladins to memorize spells they have access to
/// through prestige class caster level boosts.
/// </summary>
[ServiceBinding(typeof(PrestigeDivineSpellbookService))]
public class PrestigeDivineSpellbookService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly WindowDirector _windowDirector;

    public PrestigeDivineSpellbookService(WindowDirector windowDirector)
    {
        _windowDirector = windowDirector;
    }

    /// <summary>
    /// Opens the Prestige Divine Spellbook window for a player to memorize spells.
    /// TEMPORARILY DISABLED - Window needs further development
    /// </summary>
    public void OpenSpellbook(NwPlayer player, ClassType classType)
    {
        // DISABLED: This feature is still in development
        return;

        /*
        if (player.LoginCreature == null)
        {
            Log.Warn($"Cannot open spellbook: player has no login creature");
            return;
        }

        if (classType is not (ClassType.Ranger or ClassType.Paladin))
        {
            Log.Warn($"Cannot open spellbook for {classType}: only Rangers and Paladins supported");
            return;
        }

        try
        {
            var view = new PrestigeDivineSpellbookView(player, classType, player.LoginCreature);
            var presenter = view.Presenter;

            _windowDirector.OpenWindow(presenter);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to open Prestige Divine Spellbook: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
            player.SendServerMessage($"Error opening spellbook: {ex.Message}", ColorConstants.Red);
        }
        */
    }
}





