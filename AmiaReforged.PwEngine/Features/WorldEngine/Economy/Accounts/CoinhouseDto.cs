using System;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;

/// <summary>
/// Lightweight representation of a coinhouse aggregate.
/// </summary>
public sealed record CoinhouseDto
{
    public required long Id { get; init; }
    public required CoinhouseTag Tag { get; init; }
    public required int Settlement { get; init; }
    public required Guid EngineId { get; init; }
    public required PersonaId Persona { get; init; }
}
