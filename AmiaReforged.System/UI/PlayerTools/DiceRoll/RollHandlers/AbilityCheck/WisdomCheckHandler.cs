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
        
        string message = $"<c � >[?] <c f�>Wisdom Check</c> = D20: </c><c�  >{roll}</c><c � > + Wisdom Modifier ( <c�  >{wisMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
    }
}