using AmiaReforged.PwEngine.Tests.Systems.WorldEngine;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

public interface ICharacterRepository
{
    void Add(ICharacter character);
    bool Exists(Guid membershipCharacterId);
    void Delete(TestCharacter character);
    ICharacter? GetById(Guid characterId);
}
