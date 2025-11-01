using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

/// <summary>
/// Provides enriched persona projections with ownership metadata for UI and workflow layers.
/// </summary>
[ServiceBinding(typeof(IPersonaDescriptorService))]
public sealed class PersonaDescriptorService : IPersonaDescriptorService
{
    private static readonly IReadOnlyList<string> EmptyOwners = Array.Empty<string>();

    private readonly IPersonaRepository _personas;
    private readonly IPersistentCharacterRepository _characters;

    public PersonaDescriptorService(IPersonaRepository personas, IPersistentCharacterRepository characters)
    {
        _personas = personas;
        _characters = characters;
    }

    public PersonaDescriptor Describe(PersonaId personaId)
    {
        Persona persona = _personas.GetPersona(personaId);
        IReadOnlyList<string> owners = ResolveOwners(persona);
        return PersonaDescriptor.FromPersona(persona, owners);
    }

    public IReadOnlyList<PersonaDescriptor> DescribeMany(IEnumerable<PersonaId> personaIds)
    {
        PersonaId[] ids = personaIds?.ToArray() ?? Array.Empty<PersonaId>();
        if (ids.Length == 0)
            return Array.Empty<PersonaDescriptor>();

        Dictionary<PersonaId, Persona> resolved = _personas.GetPersonas(ids);
        if (resolved.Count == 0)
            return Array.Empty<PersonaDescriptor>();

        List<PersonaDescriptor> descriptors = new List<PersonaDescriptor>(resolved.Count);
        foreach (PersonaId id in ids)
        {
            if (!resolved.TryGetValue(id, out Persona? persona))
                continue;

            descriptors.Add(PersonaDescriptor.FromPersona(persona, ResolveOwners(persona)));
        }

        return descriptors;
    }

    public bool TryDescribe(PersonaId personaId, out PersonaDescriptor? descriptor)
    {
        descriptor = null;
        if (!_personas.TryGetPersona(personaId, out Persona? persona) || persona is null)
            return false;

        descriptor = PersonaDescriptor.FromPersona(persona, ResolveOwners(persona));
        return true;
    }

    private IReadOnlyList<string> ResolveOwners(Persona persona)
    {
        return persona switch
        {
            CharacterPersona character => ResolveCharacterOwner(character.CharacterId),
            _ => EmptyOwners
        };
    }

    private IReadOnlyList<string> ResolveCharacterOwner(CharacterId characterId)
    {
        PersistedCharacter? record = _characters.GetByGuid(characterId.Value);
        if (record is null)
            return EmptyOwners;

        string cdKey = record.CdKey?.Trim() ?? string.Empty;
        return string.IsNullOrEmpty(cdKey) ? EmptyOwners : new[] { cdKey };
    }
}
