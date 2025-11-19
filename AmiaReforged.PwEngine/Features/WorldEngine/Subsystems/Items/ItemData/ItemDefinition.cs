using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

public record ItemBlueprint(
    string ResRef,
    string ItemTag,
    string Name,
    string Description,
    MaterialEnum[] Materials,
    JobSystemItemType JobSystemType,
    int BaseItemType,
    AppearanceData Appearance,
    IReadOnlyList<JsonLocalVariableDefinition>? LocalVariables = null,
    int BaseValue = 1,
    int WeightIncreaseConstant = -1);
