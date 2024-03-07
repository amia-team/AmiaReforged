using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.Special;

[DiceRoll(DiceRollType.RollInitiative)]
public class RollInitiativeHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int dexMod = playerCreature.GetAbilityModifier(Ability.Dexterity);
        int result = roll + dexMod;
        
        string message = $"<c � >[?] <c f�>Initiative Roll</c> = D20: </c><c�  >{roll}</c><c � > + Dexterity Modifier ( <c�  >{dexMod}</c><c � > ) = <c�  >{result}</c><c � > [?]</c>";
        
        playerCreature.SpeakString(message);
        
    }
}