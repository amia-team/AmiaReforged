using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the area ResRef from the encounter context.
/// </summary>
public class GetAreaResRefExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.area_resref";
    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        string resref = context.EncounterContext?.AreaResRef ?? string.Empty;

        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["area_resref"] = resref
        }));
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Area ResRef",
        Category = "Getters",
        Description = "Returns the ResRef of the current area.",
        ColorClass = "node-getter",
        ScriptCategory = GlyphScriptCategory.Encounter,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
