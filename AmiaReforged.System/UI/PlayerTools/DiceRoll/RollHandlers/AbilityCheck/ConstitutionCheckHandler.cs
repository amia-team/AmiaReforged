using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Constitution)]
public class ConstitutionCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int conMod = playerCreature.GetAbilityModifier(Ability.Constitution);
        
        int result = roll + conMod;
        
        string message = $"[?] Constitution Check = D20: {roll} + Constitution Modifier ( {conMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}