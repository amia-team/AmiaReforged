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
        
        string charListen = $"[?] Counter Bluff Listen Skill Check = D20: {roll}";
        string listenMod = $" + Listen Modifier: {modifier}";
        
        if(player.LoginCreature == null) return;
        
        player.LoginCreature.SpeakString($"{charListen} {listenMod} = {roll + modifier} [?]");
    }
}