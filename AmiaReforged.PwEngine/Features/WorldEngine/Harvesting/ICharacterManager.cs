using AmiaReforged.PwEngine.Features.WorldEngine.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting;

public interface ICharacterManager
{
    ICharacter? GetCharacter(Guid id);
}
