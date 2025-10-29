using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Queries;

/// <summary>
/// Query to get a trait definition by its tag.
/// </summary>
public sealed record GetTraitDefinitionQuery(TraitTag TraitTag) : IQuery<Trait?>;

