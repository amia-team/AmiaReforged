using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

public interface IHarvestPrecondition
{
    string Type { get; }
    bool IsMet(ICharacter character);
}
