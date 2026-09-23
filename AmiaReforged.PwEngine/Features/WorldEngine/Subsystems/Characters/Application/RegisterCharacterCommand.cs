using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Application;

/// <summary>
/// Persists a new character persona discovered on area entry, or backfills a
/// missing persona identifier for an already-persisted character.
/// </summary>
public sealed record RegisterCharacterCommand(
    CharacterId CharacterId,
    string FirstName,
    string LastName,
    string CdKey) : ICommand;
