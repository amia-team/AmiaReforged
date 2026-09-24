using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;

/// <summary>
/// Query to calculate the aggregated mechanical effects a character receives from their traits.
/// Only active and confirmed traits whose definition is registered contribute; missing
/// definitions are skipped. Stat modifiers from matching effect types are summed per key.
/// </summary>
public sealed record CalculateTraitEffectsQuery(CharacterId CharacterId) : IQuery<TraitEffectsSummary>;
