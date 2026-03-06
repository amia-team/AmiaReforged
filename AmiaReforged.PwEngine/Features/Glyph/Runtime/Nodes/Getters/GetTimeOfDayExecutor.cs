using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the current NWN game time as total hours (a float).
/// </summary>
public class GetTimeOfDayExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.time_of_day";
    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["hours"] = context.EncounterContext.GameTime.TotalHours
        }));
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Time of Day",
        Category = "Getters",
        Description = "Returns the current NWN game time as total hours (0.0 – 24.0).",
        ColorClass = "node-getter",
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "hours", Name = "Hours", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Output }
        ]
    };
}
