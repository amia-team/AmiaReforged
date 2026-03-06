using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the party size from the encounter context.
/// </summary>
public class GetPartySizeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.party_size";
    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["party_size"] = context.EncounterContext!.PartySize
        }));
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Party Size",
        Category = "Getters",
        Description = "Returns the number of party members in the encounter area.",
        ColorClass = "node-getter",
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "party_size", Name = "Party Size", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
