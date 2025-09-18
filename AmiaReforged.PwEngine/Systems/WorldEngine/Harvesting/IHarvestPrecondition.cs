using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public interface IHarvestPrecondition
{
    string Type { get; }
    bool IsMet(ICharacter character);
}
