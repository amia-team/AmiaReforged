namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Represents an automated system process as a persona/actor in the world system.
/// Examples: tax collection, decay, market rebalancing, automated rewards.
/// </summary>
public sealed record SystemPersona : Persona
{
    /// <summary>
    /// The unique identifier/name for this system process.
    /// </summary>
    public required string ProcessName { get; init; }

    /// <summary>
    /// Creates a new SystemPersona for an automated process.
    /// </summary>
    public static SystemPersona Create(string processName, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(processName))
            throw new ArgumentException("Process name cannot be empty", nameof(processName));

        SystemPersona persona = new SystemPersona
        {
            Id = PersonaId.FromSystem(processName),
            Type = PersonaType.SystemProcess,
            DisplayName = displayName ?? $"System: {processName}",
            ProcessName = processName
        };

        persona.ValidateTypeConsistency();
        return persona;
    }
}

