using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Requests that a player persona be observed (upserted) in the persistent
/// persona repository.
/// </summary>
/// <remarks>
/// The caller captures identity at the NWN boundary and passes it as plain
/// values. Constructing the <c>NwPlayer</c> and reading its properties remains
/// the responsibility of <see cref="RuntimeCharacterService"/>.
/// </remarks>
public sealed record ObservePlayerPersonaCommand(
    string CdKey,
    string DisplayName,
    DateTime ObservedUtc) : ICommand;
