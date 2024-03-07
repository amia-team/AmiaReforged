using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers;

[DiceRoll(DiceRollType.CounterBluffListen)]
public class CounterBluffListenHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        int roll = NWScript.d20();
        int modifier = player.LoginCreature.GetSkillRank(NwSkill.FromSkillType(Skill.Listen)!);
        
        string charListen = $"<c þ >[?] </c><c fþ>Counter Bluff Listen Skill Check = D20:</c> <cþ  >{roll}</c>";
        string listenMod = $"<c þ > + Listen Modifier:</c> <cþ  >{modifier}</c>";
        
        if(player.LoginCreature == null) return;
        
        player.LoginCreature.SpeakString($"{charListen} {listenMod}<c þ > = </c><cþ  >{roll + modifier}</c><c þ > [?]</c>");
    }
}