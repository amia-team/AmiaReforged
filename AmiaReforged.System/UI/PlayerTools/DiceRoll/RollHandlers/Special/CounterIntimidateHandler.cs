using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.CounterIntimidate)]
public class CounterIntimidateHandler : IRollHandler
{
    /// <summary>
    /// Counter Intimidate using 3.0 rules
    /// </summary>
    /// <param name="player"></param>
    public void RollDice(NwPlayer player)
    {
        int roll = NWScript.d20();
        int modifier = NWScript.GetHitDice(player.LoginCreature);
        int wisMod = NWScript.GetAbilityModifier(NWScript.ABILITY_WISDOM, player.LoginCreature);

        string charIntimidate = $"<c þ >[?] </c><c fþ>Counter Intimidate Skill Check = D20:</c> <cþ  >{roll}</c>";
        string characterLevel = $"<c þ > + Character Level:</c> <cþ  >{modifier}</c>";
        string wisdomMod = $"<c þ > + Wisdom Modifier:</c> <cþ  >{wisMod}</c>";
        
        if(player.LoginCreature == null) return;
        
        player.LoginCreature.SpeakString($"{charIntimidate} {characterLevel} {wisdomMod}<c þ > = </c><cþ  >{roll + modifier + wisMod}</c><c þ > [?]</c>");
            
    }
}