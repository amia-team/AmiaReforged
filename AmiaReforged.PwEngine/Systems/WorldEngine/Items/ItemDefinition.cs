using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public record ItemDefinition(
    string ResRef,
    string ItemTag,
    string Name,
    string Description,
    MaterialEnum[] Materials,
    JobSystemItemType JobSystemType,
    int BaseItemType,
    AppearanceData Appearance,
    int BaseValue = 1);
