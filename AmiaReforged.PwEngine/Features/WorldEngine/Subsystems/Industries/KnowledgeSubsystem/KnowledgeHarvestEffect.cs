using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Defines a modifier that a piece of <see cref="Knowledge"/> applies to a resource node during harvesting.
/// The <see cref="NodeTag"/> supports wildcards — see <see cref="NodeTagPattern"/> for details.
/// </summary>
public record KnowledgeHarvestEffect(
    NodeTagPattern NodeTag,
    HarvestStep StepModified,
    float Value,
    EffectOperation Operation);
