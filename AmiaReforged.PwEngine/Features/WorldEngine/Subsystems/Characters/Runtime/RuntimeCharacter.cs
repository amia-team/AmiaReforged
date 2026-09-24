using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Services;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;

/// <summary>
/// Runtime specific implementation for a character. Acts as a facade for various character services
/// </summary>
public class RuntimeCharacter(
    CharacterId characterId,
    IInventoryPort inventoryPort,
    ICharacterSheetPort characterSheetPort,
    IIndustryMembershipService membershipService,
    ICharacterStatService statService,
    ICommandDispatcher dispatcher) : ICharacter
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
        CommandResult result = dispatcher
            .DispatchAsync(new LearnKnowledgeCommand
            {
                CharacterId = characterId,
                KnowledgeTag = knowledgeTag
            }, CancellationToken.None).GetAwaiter().GetResult();

        return result.Data != null && result.Data.TryGetValue("result", out object? value) && value is LearningResult learningResult
            ? learningResult
            : LearningResult.CharacterNotFound;
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

    /// <inheritdoc />
    public KnowledgeProgression GetProgression()
    {
        IKnowledgeProgressionService progressionService = AnvilCore.GetService<IKnowledgeProgressionService>()!;
        return progressionService.GetProgression(characterId);
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
        // Route through the shared enrollment command so membership is created exactly once,
        // through the dispatcher (duplicate/unknown handling stays centralized in the handler).
        CommandResult result = dispatcher.DispatchAsync(
            new EnrollInIndustryCommand
            {
                CharacterId = characterId,
                IndustryTag = new IndustryTag(industryTag)
            }).GetAwaiter().GetResult();

        if (!result.Success)
        {
            Log.Warn("Industry enrollment failed for character {Character}: {Reason}",
                characterId, result.ErrorMessage);
        }
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
        ICommandDispatcher dispatcher = AnvilCore.GetService<ICommandDispatcher>()!;
        IInventoryPort inventoryPort = RuntimeInventoryPort.For(creature);
        ICharacterSheetPort characterSheetPort = RuntimeCharacterSheetPort.For(creature);

        return new RuntimeCharacter(CharacterId.From(creature.UUID), inventoryPort, characterSheetPort, memberships, stats, dispatcher);
    }
}
