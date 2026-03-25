using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.Chat.Commands;
using NLog;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// Chat command to open the Prestige Divine Spellbook UI for Rangers and Paladins.
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PrestigeSpellbookCommand : IChatCommand
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly PrestigeDivineSpellbookService _spellbookService;

    public string Command => "prestige_spellbook";
    public string Description => "Opens the Prestige Divine Spellbook to memorize spells granted through prestige class caster level boosts.";
    public string AllowedRoles => "Player";

    public PrestigeSpellbookCommand(PrestigeDivineSpellbookService spellbookService)
    {
        _spellbookService = spellbookService;
    }

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        try
        {
            if (caller.LoginCreature == null)
            {
                caller.SendServerMessage("Error: No login creature found.", ColorConstants.Red);
                return;
            }

            // Determine which class to open the spellbook for
            ClassType classType = ClassType.Invalid;

            if (args.Length > 0)
            {
                // Parse which class from argument
                string classArg = args[0].ToLower();
                if (classArg.Contains("ranger"))
                    classType = ClassType.Ranger;
                else if (classArg.Contains("paladin"))
                    classType = ClassType.Paladin;
            }

            // If no argument or invalid, auto-detect which class needs the prestige spellbook
            if (classType == ClassType.Invalid)
            {
                var rangerInfo = caller.LoginCreature.GetClassInfo(ClassType.Ranger);
                var paladinInfo = caller.LoginCreature.GetClassInfo(ClassType.Paladin);

                int rangerLevel = rangerInfo?.Level ?? 0;
                int paladinLevel = paladinInfo?.Level ?? 0;

                // Check which class has a prestige boost
                int rangerEffectiveCL = rangerLevel > 0
                    ? EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(caller.LoginCreature, ClassType.Ranger)
                    : 0;
                int paladinEffectiveCL = paladinLevel > 0
                    ? EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(caller.LoginCreature, ClassType.Paladin)
                    : 0;

                // Determine which class to open
                if (rangerLevel > 0 && rangerEffectiveCL > rangerLevel)
                {
                    // Ranger has prestige boost
                    classType = ClassType.Ranger;
                }
                else if (paladinLevel > 0 && paladinEffectiveCL > paladinLevel)
                {
                    // Paladin has prestige boost
                    classType = ClassType.Paladin;
                }
                else
                {
                    caller.SendServerMessage(
                        "You must be a Ranger or Paladin to use the Prestige Divine Spellbook.",
                        ColorConstants.Orange);
                    return;
                }
            }

            // Verify character has the class
            var classInfo = caller.LoginCreature.GetClassInfo(classType);
            if (classInfo == null || classInfo.Level == 0)
            {
                caller.SendServerMessage(
                    $"You don't have the {classType} class.",
                    ColorConstants.Orange);
                return;
            }

            // Check if they have an effective caster level boost
            int effectiveLevel = EffectiveCasterLevelCalculator.GetEffectiveCasterLevelForClass(caller.LoginCreature, classType);
            int actualLevel = classInfo.Level;

            if (effectiveLevel <= actualLevel)
            {
                caller.SendServerMessage(
                    $"You don't have any prestige caster level boosts for {classType}.",
                    ColorConstants.Orange);
                return;
            }

            // Open the spellbook
            _spellbookService.OpenSpellbook(caller, classType);
            Log.Info($"{caller.LoginCreature.Name} opened Prestige Divine Spellbook for {classType}");
        }
        catch (Exception ex)
        {
            Log.Error($"Error in PrestigeSpellbookCommand: {ex.Message}");
            Log.Error($"Stack trace: {ex.StackTrace}");
            caller.SendServerMessage($"Error: {ex.Message}", ColorConstants.Red);
        }
    }
}




