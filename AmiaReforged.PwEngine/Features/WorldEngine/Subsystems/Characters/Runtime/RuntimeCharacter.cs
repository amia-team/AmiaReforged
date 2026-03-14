using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;

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
    private readonly Dictionary<string, List<CraftingModifier>> _craftingModifierCache = new();
    private HashSet<string>? _unlockedInteractionCache;

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

    public List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag, ResourceType resourceType)
    {
        if (_nodeEffectCache.TryGetValue(definitionTag, out List<KnowledgeHarvestEffect>? effects))
        {
            return effects;
        }

        List<KnowledgeHarvestEffect> knowledgeEffectsForResource = AllKnowledge()
            .SelectMany(knowledge => knowledge.HarvestEffects
                .Where(he => he.NodeTag.Matches(definitionTag, resourceType)))
            .ToList();

        _nodeEffectCache.TryAdd(definitionTag, knowledgeEffectsForResource);

        return knowledgeEffectsForResource;
    }

    /// <inheritdoc />
    public void InvalidateEffectCache()
    {
        _nodeEffectCache.Clear();
        _craftingModifierCache.Clear();
        _unlockedInteractionCache = null;
    }

    /// <inheritdoc />
    public bool HasUnlockedInteraction(string interactionTag)
    {
        _unlockedInteractionCache ??= AllKnowledge()
            .SelectMany(k => k.Effects)
            .Where(e => e.EffectType == KnowledgeEffectType.UnlockInteraction)
            .Select(e => e.TargetTag)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _unlockedInteractionCache.Contains(interactionTag);
    }

    /// <inheritdoc />
    public List<CraftingModifier> CraftingModifiersForRecipe(string recipeId, string industryTag)
    {
        string cacheKey = $"{recipeId}|{industryTag}";
        if (_craftingModifierCache.TryGetValue(cacheKey, out List<CraftingModifier>? cached))
            return cached;

        List<CraftingModifier> modifiers = AllKnowledge()
            .SelectMany(k => k.CraftingModifiers)
            .Where(m => m.Matches(recipeId, industryTag))
            .ToList();

        _craftingModifierCache.TryAdd(cacheKey, modifiers);
        return modifiers;
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
