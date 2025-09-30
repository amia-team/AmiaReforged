using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;

public record ItemDefinition(
    string ResRef,
    string ItemTag,
    string Name,
    string Description,
    MaterialEnum[] Materials,
    JobSystemItemType JobSystemType,
    int BaseItemType,
    AppearanceData Appearance,
    int BaseValue = 1,
    int WeightIncreaseConstant = -1);
