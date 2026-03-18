using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns region information from the encounter context.
/// </summary>
public class GetRegionInfoExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.region_info";
    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        bool isInRegion = context.EncounterContext?.IsInRegion ?? false;
        string regionTag = context.EncounterContext?.RegionTag ?? string.Empty;

        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["is_in_region"] = isInRegion,
            ["region_tag"] = regionTag
        }));
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Region Info",
        Category = "Getters",
        Description = "Returns whether the encounter is in a region and the region's tag.",
        ColorClass = "node-getter",
        ScriptCategory = GlyphScriptCategory.Encounter,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "is_in_region", Name = "Is In Region", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "region_tag", Name = "Region Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
