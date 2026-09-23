using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Requests that a cached runtime <see cref="ICharacter"/> be removed from the
/// runtime character repository.
/// </summary>
/// <remarks>
/// Removal is idempotent: removing a character that is not cached succeeds and is
/// not treated as an error.
/// </remarks>
public sealed record RemoveRuntimeCharacterCommand(CharacterId CharacterId) : ICommand;
