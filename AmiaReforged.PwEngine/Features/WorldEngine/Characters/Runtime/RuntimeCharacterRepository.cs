using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;

[ServiceBinding(typeof(ICharacterRepository))]
public class RuntimeCharacterRepository : ICharacterRepository
{
    private readonly Dictionary<Guid, ICharacter> _characters = [];

    public void Add(ICharacter character)
    {
        _characters.TryAdd(character.GetId(), character);
    }

    public void DeleteById(Guid characterId)
    {
        _characters.Remove(characterId);
    }

    public ICharacter? GetById(Guid characterId)
    {
        return _characters.GetValueOrDefault(characterId);
    }

    public void Delete(ICharacter character)
    {
        _characters.Remove(character.GetId());
    }

    public bool Exists(Guid membershipCharacterId)
    {
        return _characters.ContainsKey(membershipCharacterId);
    }

    public static ICharacterRepository Create()
    {
        return new RuntimeCharacterRepository();
    }
}
