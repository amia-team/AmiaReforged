using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.CounterIntimidate)]
public class CounterIntimidateHandler : IRollHandler
{
    /// 
    /// Counter Intimidate using 3.0 rules
    /// 
    /// 
    public void RollDice(NwPlayer player)
    {
        int roll = NWScript.d20();
        int modifier = NWScript.GetHitDice(player.LoginCreature);
        int wisMod = NWScript.GetAbilityModifier(NWScript.ABILITY_WISDOM, player.LoginCreature);

        string charIntimidate = $"[?] Counter Intimidate Skill Check = D20: {roll}";
        string characterLevel = $" + Character Level: {modifier}";
        string wisdomMod = $" + Wisdom Modifier: {wisMod}";
        
        if(player.LoginCreature == null) return;
        
        player.LoginCreature.SpeakString($"{charIntimidate} {characterLevel} {wisdomMod} = {roll + modifier + wisMod} [?]");
            
    }
}