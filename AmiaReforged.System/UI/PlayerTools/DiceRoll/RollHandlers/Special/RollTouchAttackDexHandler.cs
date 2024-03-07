using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.RollTouchAttackDex)]
public class RollTouchAttackDexHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int dexMod = playerCreature.GetAbilityModifier(Ability.Dexterity);
        int sizeMod = (int)playerCreature.Size;
        int baseAttackBonus = playerCreature.BaseAttackBonus;
        
        int result = roll + dexMod + sizeMod + baseAttackBonus;
        
        string message = $"[?] Dexterity Touch Attack = D20: {roll} + Dexterity Modifier ( {dexMod} ) + Size Modifier ( {sizeMod} ) + Base Attack Bonus ( {baseAttackBonus} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}