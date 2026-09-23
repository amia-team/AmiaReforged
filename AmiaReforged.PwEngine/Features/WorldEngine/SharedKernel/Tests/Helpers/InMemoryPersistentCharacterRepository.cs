using System.Collections.Generic;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;

/// <summary>
/// In-memory test double for <see cref="IPersistentCharacterRepository"/>
/// that records mutation calls so handlers can be verified.
/// </summary>
public sealed class InMemoryPersistentCharacterRepository : IPersistentCharacterRepository
{
    private readonly Dictionary<Guid, PersistedCharacter> _characters = new();
    public List<PersistedCharacter> AddedCharacters { get; } = new();
    public List<(Guid CharacterId, string PersonaIdString)> PersonaIdUpdates { get; } = new();

    public void AddCharacter(PersistedCharacter character)
    {
        _characters[character.Id] = character;
        AddedCharacters.Add(character);
    }

    public List<PersistedCharacter> GetCharacters()
    {
        return _characters.Values.ToList();
    }

    public List<PersistedCharacter> GetCharactersByCdKey(string cdKey)
    {
        return _characters.Values.Where(c => c.CdKey == cdKey).ToList();
    }

    public PersistedCharacter? GetByGuid(Guid id)
    {
        return _characters.GetValueOrDefault(id);
    }

    public void UpdatePersonaId(Guid characterId, string personaIdString)
    {
        if (_characters.TryGetValue(characterId, out PersistedCharacter? character))
        {
            PersonaIdUpdates.Add((characterId, personaIdString));
            character.PersonaIdString = personaIdString;
        }
    }

    public void ChangeCharacterOwner(PersistedCharacter character, string cdKey)
    {
        character.CdKey = cdKey;
    }

    public void DeleteCharacter(PersistedCharacter character)
    {
        _characters.Remove(character.Id);
    }

    public void SaveChanges()
    {
    }

    public PersistedCharacter? GetByPersonaId(PersonaId persona)
    {
        return _characters.Values.FirstOrDefault(c => c.PersonaIdString == persona.ToString());
    }
}
