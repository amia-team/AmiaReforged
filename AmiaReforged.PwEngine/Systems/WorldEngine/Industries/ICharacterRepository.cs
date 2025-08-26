using AmiaReforged.PwEngine.Tests.Systems.WorldEngine;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface ICharacterRepository
{
    void Add(ICharacter character);
    bool Exists(Guid membershipCharacterId);
    void Delete(TestCharacter character);
    ICharacter? GetById(Guid characterId);
}
