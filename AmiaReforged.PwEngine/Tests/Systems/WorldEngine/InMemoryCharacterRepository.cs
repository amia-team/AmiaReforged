using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

public class InMemoryCharacterRepository : ICharacterRepository
{
    private readonly List<ICharacter> _characters = [];

    public void Add(ICharacter character)
    {
        _characters.Add(character);
    }

    public ICharacter? GetById(Guid characterId)
    {
        return _characters.FirstOrDefault(c => c.GetId() == characterId);
    }

    public void Delete(TestCharacter character)
    {
        try
        {
            _characters.Remove(character);
        }
        catch (Exception e)
        {
            // Nothin'
        }
    }

    public bool Exists(Guid membershipCharacterId)
    {
        return _characters.Any(c => c.GetId() == membershipCharacterId);
    }

    public static ICharacterRepository Create()
    {
        return new InMemoryCharacterRepository();
    }
}