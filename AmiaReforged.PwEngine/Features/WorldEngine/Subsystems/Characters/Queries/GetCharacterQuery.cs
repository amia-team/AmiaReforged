using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;

/// <summary>
/// Looks up a character by id from the runtime character repository.
/// </summary>
/// <remarks>
/// This is the single read path for character identity used by the character
/// subsystem and the runtime service. Callers that need a non-null result and a
/// documented failure should inspect the returned value (see
/// <see cref="CharacterSubsystem.GetKnowledgeContext(CharacterId)"/> and
/// <see cref="CharacterSubsystem.GetIndustryContext(CharacterId)"/>).
/// </remarks>
public sealed record GetCharacterQuery(CharacterId CharacterId) : IQuery<ICharacter?>;
