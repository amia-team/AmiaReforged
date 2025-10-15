using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Constitution)]
public class ConstitutionCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int conMod = playerCreature.GetAbilityModifier(Ability.Constitution);

        playerCreature.SpeakString(new AbilityCheckString(abilityName: "Constitution", roll, conMod)
            .GetAbilityCheckString());
    }
}
