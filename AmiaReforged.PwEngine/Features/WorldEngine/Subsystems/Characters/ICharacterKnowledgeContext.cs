using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

public interface ICharacterKnowledgeContext
{
    int GetKnowledgePoints();
    void AddKnowledgePoints(int points);
    void SubtractKnowledgePoints(int points);
    List<Knowledge> AllKnowledge();
    LearningResult Learn(string knowledgeTag);
    bool CanLearn(string knowledgeTag);
    List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag, ResourceType resourceType);

    /// <summary>
    /// Clears any cached harvest-effect lookups, forcing re-evaluation of wildcard patterns
    /// on the next call to <see cref="KnowledgeEffectsForResource"/>.
    /// </summary>
    void InvalidateEffectCache();

    /// <summary>
    /// Returns all <see cref="CraftingModifier"/>s from the character's learned knowledge
    /// that match the given recipe/industry combination.
    /// </summary>
    List<CraftingModifier> CraftingModifiersForRecipe(string recipeId, string industryTag);

    /// <summary>
    /// Returns <c>true</c> if the character has learned knowledge whose effects include
    /// <see cref="KnowledgeEffectType.UnlockInteraction"/> targeting <paramref name="interactionTag"/>.
    /// </summary>
    bool HasUnlockedInteraction(string interactionTag);
}
