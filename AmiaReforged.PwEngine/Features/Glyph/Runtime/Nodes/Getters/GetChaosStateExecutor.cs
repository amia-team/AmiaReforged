using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns all four chaos state axes (Danger, Corruption, Density, Mutation) from the encounter context.
/// </summary>
public class GetChaosStateExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.chaos_state";
    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["danger"] = context.EncounterContext!.Chaos.Danger,
            ["corruption"] = context.EncounterContext.Chaos.Corruption,
            ["density"] = context.EncounterContext.Chaos.Density,
            ["mutation"] = context.EncounterContext.Chaos.Mutation
        }));
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Chaos State",
        Category = "Getters",
        Description = "Returns the four chaos axes (Danger, Corruption, Density, Mutation) for the current encounter's region.",
        ColorClass = "node-getter",
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "danger", Name = "Danger", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "corruption", Name = "Corruption", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "density", Name = "Density", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "mutation", Name = "Mutation", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
