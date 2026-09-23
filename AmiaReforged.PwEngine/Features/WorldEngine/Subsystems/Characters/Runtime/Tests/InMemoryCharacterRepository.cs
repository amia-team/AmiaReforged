using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using Moq;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime.Tests;

/// <summary>
/// In-memory test double for <see cref="ICharacterRepository"/> that records
/// <see cref="Add"/> calls and tracks cached characters by id. Requires no
/// live NWN objects.
/// </summary>
public sealed class InMemoryCharacterRepository : ICharacterRepository
{
    private readonly Dictionary<Guid, ICharacter> _characters = new();

    public List<ICharacter> AddedCharacters { get; } = new();

    public List<Guid> DeletedIds { get; } = new();

    public void Add(ICharacter character)
    {
        _characters.TryAdd(character.GetId(), character);
        AddedCharacters.Add(character);
    }

    public bool Exists(Guid membershipCharacterId)
    {
        return _characters.ContainsKey(membershipCharacterId);
    }

    public ICharacter? GetById(Guid characterId)
    {
        return _characters.GetValueOrDefault(characterId);
    }

    public void Delete(ICharacter character)
    {
        _characters.Remove(character.GetId());
    }

    public void DeleteById(Guid characterId)
    {
        _characters.Remove(characterId);
        DeletedIds.Add(characterId);
    }
}
