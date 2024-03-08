using System.Text;
using Anvil.API;
using NWN.Core;
using static AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.AmiaColors;
using static Anvil.API.ColorConstants;

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
        
        StringBuilder builder = new StringBuilder();
        
        string message = $"<c{AmiaLime.ToColorToken()}>[?]</c><c{LightBlue.ToColorToken()}> Initiative Roll = D20:</c> {roll}<c{LightBlue.ToColorToken()}> + Dexterity Modifier (</c> <c{Yellow.ToColorToken()}>{dexMod}</c><c{LightBlue.ToColorToken()}> ) =</c> {result} <c{AmiaLime.ToColorToken()}>[?]</c>";
        
        playerCreature.SpeakString(message);
        
    }
}