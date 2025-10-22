namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

public interface ICharacterTraitRepository
{
    List<CharacterTrait> GetByCharacterId(Guid characterId);
    void Add(CharacterTrait trait);
    void Update(CharacterTrait trait);
    void Delete(Guid traitId);
}
