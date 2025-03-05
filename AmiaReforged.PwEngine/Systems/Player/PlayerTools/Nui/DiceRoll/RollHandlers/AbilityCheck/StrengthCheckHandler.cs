using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Strength)]
public class StrengthCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int strMod = playerCreature.GetAbilityModifier(Ability.Strength);

        playerCreature.SpeakString(
            new AbilityCheckString(abilityName: "Strength", roll, strMod).GetAbilityCheckString());
    }
}