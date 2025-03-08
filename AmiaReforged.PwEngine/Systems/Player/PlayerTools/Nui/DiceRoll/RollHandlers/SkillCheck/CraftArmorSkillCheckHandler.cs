using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.DiceRoll.RollHandlers.SkillCheck;

[DiceRoll(DiceRollType.CraftArmor)]
public class CraftArmorSkillCheckHandler : IRollHandler
{
    public void RollDice(NwPlayer player)
    {
        NwCreature? playerCreature = player.LoginCreature;
        if (playerCreature is null) return;

        int roll = NWScript.d20();
        int craftArmorMod = playerCreature.GetSkillRank(Skill.CraftArmor!);

        int result = roll + craftArmorMod;

        playerCreature.SpeakString(new SkillCheckString(skillName: "Craft Armor", roll, craftArmorMod, result)
            .GetRollResult());
    }
}