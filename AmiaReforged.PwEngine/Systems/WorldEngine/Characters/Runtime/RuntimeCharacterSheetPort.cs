using AmiaReforged.PwEngine.Systems.WorldEngine.Characters.CharacterData;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters.Runtime;

public class RuntimeCharacterSheetPort(NwCreature creature) : ICharacterSheetPort
{
    // TODO: Refactor to use a dictionary...
    public List<SkillData> GetSkills()
    {
        List<SkillData> skills = [];

        skills.Add(new SkillData(Skill.AnimalEmpathy, creature.GetSkillRank(Skill.AnimalEmpathy!)));
        skills.Add(new SkillData(Skill.Appraise, creature.GetSkillRank(Skill.Appraise!)));
        skills.Add(new SkillData(Skill.Bluff, creature.GetSkillRank(Skill.Bluff!)));
        skills.Add(new SkillData(Skill.Concentration, creature.GetSkillRank(Skill.Concentration!)));
        skills.Add(new SkillData(Skill.CraftTrap, creature.GetSkillRank(Skill.CraftTrap!)));
        skills.Add(new SkillData(Skill.CraftWeapon, creature.GetSkillRank(Skill.CraftWeapon!)));
        skills.Add(new SkillData(Skill.DisableTrap, creature.GetSkillRank(Skill.DisableTrap!)));
        skills.Add(new SkillData(Skill.Discipline, creature.GetSkillRank(Skill.Discipline!)));
        skills.Add(new SkillData(Skill.Heal, creature.GetSkillRank(Skill.Heal!)));
        skills.Add(new SkillData(Skill.Hide, creature.GetSkillRank(Skill.Hide!)));
        skills.Add(new SkillData(Skill.Intimidate, creature.GetSkillRank(Skill.Intimidate!)));
        skills.Add(new SkillData(Skill.Listen, creature.GetSkillRank(Skill.Listen!)));
        skills.Add(new SkillData(Skill.Lore, creature.GetSkillRank(Skill.Lore!)));
        skills.Add(new SkillData(Skill.MoveSilently, creature.GetSkillRank(Skill.MoveSilently!)));

        return skills;
    }

    public static RuntimeCharacterSheetPort For(NwCreature creature)
    {
        return new RuntimeCharacterSheetPort(creature);
    }
}