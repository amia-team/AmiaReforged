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

public record AppearanceData(int ModelType, int? SimpleModelNumber, WeaponPartData? Data);

public record WeaponPartData(
    int TopPartModel,
    int MiddlePartModel,
    int BottomPartModel,
    int TopPartColor,
    int MiddlePartColor,
    int BottomPartColor);
