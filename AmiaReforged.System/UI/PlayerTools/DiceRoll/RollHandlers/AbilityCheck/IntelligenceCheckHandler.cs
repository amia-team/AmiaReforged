using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AbilityCheck;

[DiceRoll(DiceRollType.Intelligence)]
public class IntelligenceCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int intMod = playerCreature.GetAbilityModifier(Ability.Intelligence);
        
        int result = roll + intMod;
        
        string message = $"<c � >[?] <c f�>Intelligence Check</c> = D20: </c><c�  >{roll}</c><c � > + Intelligence Modifier ( <c�  >{intMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}