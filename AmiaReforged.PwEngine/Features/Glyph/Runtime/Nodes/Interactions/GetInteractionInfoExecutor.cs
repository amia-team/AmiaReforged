using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Pure data node that exposes the current interaction context as output pins.
/// Available in all Interaction event types. Reads from <see cref="GlyphExecutionContext"/>.
/// </summary>
public class GetInteractionInfoExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.get_info";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        Dictionary<string, object?> outputs = new Dictionary<string, object?>
        {
            ["tag"] = context.InteractionTag ?? string.Empty,
            ["target_id"] = context.InteractionTargetId.ToString(),
            ["target_mode"] = context.InteractionTargetMode ?? string.Empty,
            ["area_resref"] = context.InteractionAreaResRef ?? string.Empty,
            ["progress"] = context.InteractionProgress,
            ["required_rounds"] = context.InteractionRequiredRounds,
            ["proficiency"] = context.InteractionProficiency ?? string.Empty
        };

        return Task.FromResult(GlyphNodeResult.Data(outputs));
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Interaction Info",
        Category = "Interactions",
        Description = "Reads the current interaction context: tag, target, area, progress, and proficiency. " +
                      "Available in all Interaction event scripts.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_id", Name = "Target ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_mode", Name = "Target Mode", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "progress", Name = "Progress", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "required_rounds", Name = "Required Rounds", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
