using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;

/// <summary>
/// Query to get all available trait definitions.
/// </summary>
public sealed record GetAllTraitsQuery : IQuery<List<Trait>>;

/// <summary>
/// Query to get all traits selected by a character.
/// </summary>
public sealed record GetCharacterTraitsQuery(CharacterId CharacterId) : IQuery<List<CharacterTrait>>;

