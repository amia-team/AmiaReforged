using Anvil.API;
using NWN.Core;

namespace AmiaReforged.System.UI.PlayerTools.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.Listen)]
public class ListenSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;
        
        int roll = NWScript.d20();
        int listenMod = playerCreature.GetSkillRank(Skill.Listen!);
        
        int result = roll + listenMod;
        
        string message = $"[?] Listen Skill Check = D20: {roll} + Listen Modifier ( {listenMod} ) = {result} [?]";
        
        playerCreature.SpeakString(message);
    }
}