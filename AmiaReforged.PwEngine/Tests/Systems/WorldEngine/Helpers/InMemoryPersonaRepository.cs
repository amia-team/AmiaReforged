using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;

/// <summary>
/// In-memory implementation of IPersonaRepository for testing.
/// </summary>
public class InMemoryPersonaRepository : IPersonaRepository
{
    private readonly List<Persona> _personas = [];

    public bool TryGetPersona(PersonaId personaId, out Persona? persona)
    {
        persona = _personas.FirstOrDefault(p => p.Id.Equals(personaId));
        return persona != null;
    }

    public Persona GetPersona(PersonaId personaId)
    {
        Persona? persona = _personas.FirstOrDefault(p => p.Id.Equals(personaId));
        if (persona == null)
            throw new InvalidOperationException($"Persona {personaId} not found");
        return persona;
    }

    public bool Exists(PersonaId id)
    {
        return _personas.Any(p => p.Id.Equals(id));
    }

    public string? GetDisplayName(PersonaId personaId)
    {
        Persona? persona = _personas.FirstOrDefault(p => p.Id.Equals(personaId));
        return persona?.DisplayName;
    }

    public Dictionary<PersonaId, Persona> GetPersonas(IEnumerable<PersonaId> personaIds)
    {
        Dictionary<PersonaId, Persona> result = new Dictionary<PersonaId, Persona>();
        foreach (PersonaId id in personaIds)
        {
            Persona? persona = _personas.FirstOrDefault(p => p.Id.Equals(id));
            if (persona != null)
            {
                result[id] = persona;
            }
        }
        return result;
    }

    public void Add(Persona persona)
    {
        _personas.Add(persona);
    }

    public void SaveChanges()
    {
        // No-op for in-memory
    }

    public static InMemoryPersonaRepository Create() => new();
}

