namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// In-memory implementation of ICharacterTraitRepository for testing
/// </summary>
public class InMemoryCharacterTraitRepository : ICharacterTraitRepository
{
    private readonly Dictionary<Guid, CharacterTrait> _traits = new();

    public List<CharacterTrait> GetByCharacterId(Guid characterId)
    {
        return _traits.Values
            .Where(t => t.CharacterId == characterId)
            .ToList();
    }

    public void Add(CharacterTrait trait)
    {
        _traits[trait.Id] = trait;
    }

    public void Update(CharacterTrait trait)
    {
        if (_traits.ContainsKey(trait.Id))
        {
            _traits[trait.Id] = trait;
        }
    }

    public void Delete(Guid traitId)
    {
        _traits.Remove(traitId);
    }

    public static InMemoryCharacterTraitRepository Create()
    {
        return new InMemoryCharacterTraitRepository();
    }
}
