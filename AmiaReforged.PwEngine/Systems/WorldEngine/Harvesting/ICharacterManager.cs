using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public interface ICharacterManager
{
    ICharacter? GetCharacter(Guid id);
}