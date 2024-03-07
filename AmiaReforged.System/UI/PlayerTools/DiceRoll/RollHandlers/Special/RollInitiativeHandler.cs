using System.Text;
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
        
        StringBuilder builder = new StringBuilder();
        
        string message = $"[?] Initiative Roll = D20: {roll} + Dexterity Modifier ( {dexMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
        
    }
}