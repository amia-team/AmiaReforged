namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

public interface ICharacterRepository
{
    void Add(ICharacter character);
    bool Exists(Guid membershipCharacterId);
    void Delete(ICharacter character);
    void DeleteById(Guid characterId);
    ICharacter? GetById(Guid characterId);
}
