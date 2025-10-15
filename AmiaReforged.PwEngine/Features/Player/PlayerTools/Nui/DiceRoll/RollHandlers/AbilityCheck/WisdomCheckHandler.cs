using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Wisdom)]
public class WisdomCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int wisMod = playerCreature.GetAbilityModifier(Ability.Wisdom);

        playerCreature.SpeakString(new AbilityCheckString(abilityName: "Wisdom", roll, wisMod).GetAbilityCheckString());
    }
}
