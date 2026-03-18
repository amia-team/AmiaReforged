using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the tag of any NWN game object. Pure data node — no execution flow.
/// </summary>
public class GetTagExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.tag";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectValue = await resolveInput("object");
        uint objectId = Convert.ToUInt32(objectValue);

        string tag = objectId != NWScript.OBJECT_INVALID
            ? NWScript.GetTag(objectId)
            : string.Empty;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["tag"] = tag
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Tag",
        Category = "Getters",
        Description = "Returns the tag of a game object.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "object", Name = "Object", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "tag", Name = "Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
