using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;

/// <summary>
/// Runtime specific implementation for a character. Acts as a facade for various character services
/// </summary>
public class RuntimeCharacter(
    CharacterId characterId,
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
            IndustryTag = new IndustryTag(industryTag),
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

    public CharacterId GetId()
    {
        return characterId;
    }

    public List<SkillData> GetSkills()
    {
        return characterSheetPort.GetSkills();
    }

    public static RuntimeCharacter? For(NwCreature creature)
    {
        IIndustryMembershipService memberships = AnvilCore.GetService<IIndustryMembershipService>()!;
        ICharacterStatService stats = AnvilCore.GetService<ICharacterStatService>()!;
        IInventoryPort inventoryPort = RuntimeInventoryPort.For(creature);
        ICharacterSheetPort characterSheetPort = RuntimeCharacterSheetPort.For(creature);

        return new RuntimeCharacter(CharacterId.From(creature.UUID), inventoryPort, characterSheetPort, memberships, stats);
    }
}
