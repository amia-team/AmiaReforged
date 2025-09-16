using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using Anvil;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

/// <summary>
/// Runtime specific implementation for a character. Acts as a facade for various character services...
/// </summary>
public class RuntimeCharacter(
    Guid characterId,
    IInventoryPort inventoryPort,
    ICharacterSheetPort characterSheetPort,
    IIndustryMembershipService membershipService,
    ICharacterStatService statService) : ICharacter
{
    private readonly Dictionary<string, List<KnowledgeHarvestEffect>> _nodeEffectCache = new();

    public int GetKnowledgePoints()
    {
        return statService.GetKnowledgePoints(characterId);
    }

    public void SubtractKnowledgePoints(int points)
    {
        int newPoints = GetKnowledgePoints() - points;
        statService.UpdateKnowledgePoints(characterId, newPoints);
    }

    public void AddKnowledgePoints(int points)
    {
        int newPoints = GetKnowledgePoints() + points;
        statService.UpdateKnowledgePoints(characterId, newPoints);
    }

    public List<Knowledge> AllKnowledge()
    {
        return membershipService.AllKnowledge(characterId);
    }

    public LearningResult Learn(string knowledgeTag)
    {
        return membershipService.LearnKnowledge(characterId, knowledgeTag);
    }

    public bool CanLearn(string knowledgeTag)
    {
        return membershipService.CanLearnKnowledge(characterId, knowledgeTag);
    }

    public List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag)
    {
        if (_nodeEffectCache.TryGetValue(definitionTag, out List<KnowledgeHarvestEffect>? effects))
        {
            return effects;
        }

        List<KnowledgeHarvestEffect> knowledgeEffectsForResource = AllKnowledge()
            .SelectMany(knowledge => knowledge.HarvestEffects
                .Where(he => he.NodeTag == definitionTag))
            .ToList();

        _nodeEffectCache.TryAdd(definitionTag, knowledgeEffectsForResource);

        return knowledgeEffectsForResource;
    }

    public void AddItem(ItemDto item)
    {
        inventoryPort.AddItem(item);
    }

    public List<ItemSnapshot> GetInventory()
    {
        return inventoryPort.GetInventory();
    }

    public Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment()
    {
        return inventoryPort.GetEquipment();
    }

    public void JoinIndustry(string industryTag)
    {
        IndustryMembership m = new()
        {
            IndustryTag = industryTag,
            Level = ProficiencyLevel.Novice,
            CharacterKnowledge = [],
            CharacterId = characterId
        };

        membershipService.AddMembership(m);
    }

    public List<IndustryMembership> AllIndustryMemberships()
    {
        return membershipService.GetMemberships(characterId);
    }

    public RankUpResult RankUp(string industryTag)
    {
        return membershipService.RankUp(characterId, industryTag);
    }

    public Guid GetId()
    {
        return characterId;
    }

    public List<SkillData> GetSkills()
    {
        return characterSheetPort.GetSkills();
    }

    public static RuntimeCharacter? For(NwCreature creature)
    {
        IIndustryMembershipService injector = AnvilCore.GetService<IIndustryMembershipService>()!;
        ICharacterStatService statService = AnvilCore.GetService<ICharacterStatService>()!;
        IInventoryPort inventoryPort = RuntimeInventoryPort.For(creature);
        ICharacterSheetPort characterSheetPort = RuntimeCharacterSheetPort.For(creature);

        return new RuntimeCharacter(creature.UUID, inventoryPort, characterSheetPort, injector, statService);
    }
}

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

public interface ICharacterSheetPort
{
    List<SkillData> GetSkills();
}

public interface IInventoryPort
{
    void AddItem(ItemDto item);
    List<ItemSnapshot> GetInventory();
    Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment();
}
