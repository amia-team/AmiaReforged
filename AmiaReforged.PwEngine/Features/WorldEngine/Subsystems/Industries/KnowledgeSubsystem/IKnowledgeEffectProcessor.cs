using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Interface for processing side-effects when a character learns knowledge.
/// Dispatches <see cref="KnowledgeEffect"/> entries to the appropriate subsystems
/// (Codex, Recipes, Harvesting, etc.).
/// </summary>
public interface IKnowledgeEffectProcessor
{
    /// <summary>
    /// Processes all effects associated with a piece of knowledge that was just learned.
    /// </summary>
    Task ProcessEffectsAsync(CharacterId characterId, Knowledge knowledge, List<KnowledgeEffect> effects);
}
