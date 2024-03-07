using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Wisdom)]
public class WisdomCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int wisMod = playerCreature.GetAbilityModifier(Ability.Wisdom);
        
        int result = roll + wisMod;
        
        string message = $"[?] Wisdom Check = D20: {roll} + Wisdom Modifier ( {wisMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}