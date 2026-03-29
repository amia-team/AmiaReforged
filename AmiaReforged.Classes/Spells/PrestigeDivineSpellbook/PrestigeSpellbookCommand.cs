using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.Chat.Commands;

namespace AmiaReforged.Classes.Spells.PrestigeDivineSpellbook;

/// <summary>
/// Chat command to open the Prestige Divine Spellbook UI for Rangers and Paladins.
/// TEMPORARILY DISABLED - Feature in development
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class PrestigeSpellbookCommand : IChatCommand
{
    public string Command => "prestige_spellbook";
    public string Description => "Opens the Prestige Divine Spellbook to memorize spells granted through prestige class caster level boosts.";
    public string AllowedRoles => "Player";

    public PrestigeSpellbookCommand(PrestigeDivineSpellbookService _)
    {
        // Unused - feature disabled
    }

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        // DISABLED: Feature in development
        return Task.CompletedTask;
    }
}





