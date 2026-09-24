using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;

/// <summary>
/// Reads the truthful statistics projection for a character.
/// </summary>
/// <remarks>
/// The projection exposes only <see cref="CharacterStats.PlayTime"/>, the single
/// supported field. It returns <c>null</c> when the character has no statistics
/// record. See task 022 for the contract.
/// </remarks>
public sealed record GetCharacterStatsQuery(CharacterId CharacterId) : IQuery<CharacterStats?>;
