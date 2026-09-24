using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;

/// <summary>
/// Query checking whether a character actively possesses a specific trait.
/// Returns false when the trait definition is missing, the character owns no
/// matching trait, or the matching trait is currently inactive.
/// </summary>
public sealed record HasTraitAsyncQuery(CharacterId CharacterId, TraitTag TraitTag) : IQuery<bool>;
