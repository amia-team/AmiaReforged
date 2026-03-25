using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// Service that provides the logic for opening the appropriate prestige divine spellbook
/// based on which class (Ranger or Paladin) has a prestige caster level boost.
/// </summary>
[ServiceBinding(typeof(PrestigeDivineSpellbookKeybindService))]
public class PrestigeDivineSpellbookKeybindService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly PrestigeDivineSpellbookService _spellbookService;

    public PrestigeDivineSpellbookKeybindService(PrestigeDivineSpellbookService spellbookService)
    {
        _spellbookService = spellbookService;
        Log.Info("PrestigeDivineSpellbookKeybindService initialized");
    }

    /// <summary>
    /// Opens the prestige divine spellbook for a player, auto-detecting which class to open.
    /// Will open Ranger if it has a prestige boost, otherwise Paladin if it has a boost.
    /// </summary>
    public void OpenPrestigeSpellbookAuto(NwPlayer player)
    {
        try
        {
            if (player.LoginCreature == null)
            {
                Log.Warn($"Cannot open prestige spellbook: player has no login creature");
                return;
            }

            var rangerInfo = player.LoginCreature.GetClassInfo(ClassType.Ranger);
            var paladinInfo = player.LoginCreature.GetClassInfo(ClassType.Paladin);

            int rangerLevel = rangerInfo?.Level ?? 0;
            int paladinLevel = paladinInfo?.Level ?? 0;

            // Check which class has a prestige boost
            int rangerEffectiveCl = rangerLevel > 0
                ? EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(player.LoginCreature, ClassType.Ranger)
                : 0;
            int paladinEffectiveCl = paladinLevel > 0
                ? EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(player.LoginCreature, ClassType.Paladin)
                : 0;

            ClassType classToOpen;

            // Determine which class to open - Ranger takes precedence
            if (rangerLevel > 0 && rangerEffectiveCl > rangerLevel)
            {
                classToOpen = ClassType.Ranger;
            }
            else if (paladinLevel > 0 && paladinEffectiveCl > paladinLevel)
            {
                classToOpen = ClassType.Paladin;
            }
            else
            {
                player.SendServerMessage(
                    "You must be a Ranger or Paladin with prestige caster level boosts to use the Prestige Divine Spellbook.",
                    ColorConstants.Orange);
                return;
            }

            _spellbookService.OpenSpellbook(player, classToOpen);
            Log.Info($"{player.LoginCreature.Name} opened Prestige Divine Spellbook (auto-detected: {classToOpen})");
        }
        catch (Exception ex)
        {
            Log.Error($"Error in OpenPrestigeSpellbookAuto: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
            player.SendServerMessage($"Error opening spellbook: {ex.Message}", ColorConstants.Red);
        }
    }
}




