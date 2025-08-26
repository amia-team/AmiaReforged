using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

public record HarvestContext(JobSystemItemType RequiredItemType, Material RequiredItemMaterial = Material.None);

public interface IHarvestPrecondition
{
    string Type { get; }
    bool IsMet(ICharacter character);
}
