using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Queries;

/// <summary>
/// Query to get a character's current trait budget status.
/// </summary>
public sealed record GetTraitBudgetQuery(CharacterId CharacterId) : IQuery<TraitBudget>;

