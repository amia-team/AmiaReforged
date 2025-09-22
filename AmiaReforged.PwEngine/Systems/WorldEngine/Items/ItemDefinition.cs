using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public record ItemDefinition(
    string ResRef,
    string ItemTag,
    string Name,
    string Description,
    Material[] Materials,
    JobSystemItemType JobSystemType,
    int BaseItemType,
    AppearanceData Appearance);
