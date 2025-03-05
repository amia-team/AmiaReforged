using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Dexterity)]
public class DexterityCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int dexMod = playerCreature.GetAbilityModifier(Ability.Dexterity);

        playerCreature.SpeakString(
            new AbilityCheckString(abilityName: "Dexterity", roll, dexMod).GetAbilityCheckString());
    }
}