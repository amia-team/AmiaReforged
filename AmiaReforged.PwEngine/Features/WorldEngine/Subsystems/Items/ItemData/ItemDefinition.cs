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
    int WeightIncreaseConstant = -1)
{
    /// <summary>
    /// The source filename (without extension) this blueprint was loaded from.
    /// Used as a fallback lookup key when ItemTag doesn't match.
    /// </summary>
    public string? SourceFile { get; init; }
}
