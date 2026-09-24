using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Commands;

/// <summary>
/// Marks a player persona as recently active in the persistent persona repository.
/// </summary>
/// <remarks>
/// The caller captures identity at the NWN boundary and passes it as plain
/// values. Constructing the <c>NwPlayer</c> and reading its properties remains
/// the responsibility of <see cref="RuntimeCharacterService"/>. The activation
/// timestamp is captured by the service at dispatch time so the value is stable
/// across the handler.
/// </remarks>
public sealed record TouchPlayerPersonaCommand(
    string CdKey,
    DateTime ActivatedUtc) : ICommand;
