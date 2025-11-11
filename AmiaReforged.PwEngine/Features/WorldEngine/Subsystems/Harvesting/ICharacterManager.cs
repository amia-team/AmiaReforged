using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

public interface ICharacterManager
{
    ICharacter? GetCharacter(Guid id);
}
