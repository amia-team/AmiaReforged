using AmiaReforged.PwEngine.Features.WorldEngine.Characters;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting;

public interface IHarvestPrecondition
{
    string Type { get; }
    bool IsMet(ICharacter character);
}
