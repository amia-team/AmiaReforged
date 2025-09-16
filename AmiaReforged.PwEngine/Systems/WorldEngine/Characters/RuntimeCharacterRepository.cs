using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

[ServiceBinding(typeof(RuntimeCharacterRepository))]
public class RuntimeCharacterRepository : ICharacterRepository
{
    private readonly Dictionary<Guid, ICharacter> _characters = [];

    public void Add(ICharacter character)
    {
        _characters.TryAdd(character.GetId(), character);
    }

    public ICharacter? GetById(Guid characterId)
    {
        return _characters.GetValueOrDefault(characterId);
    }

    public void Delete(ICharacter character)
    {
        try
        {
            _characters.Remove(character.GetId());
        }
        catch (Exception e)
        {
            // Nothin'
        }
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
