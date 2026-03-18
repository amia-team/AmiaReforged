using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the blueprint ResRef of any NWN game object. Pure data node — no execution flow.
/// Unlike <see cref="GetCreatureResRefExecutor"/> which is creature-specific, this works
/// with any object type (placeables, doors, items, etc.).
/// </summary>
public class GetObjectResRefExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.object_resref";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectValue = await resolveInput("object");
        uint objectId = Convert.ToUInt32(objectValue);

        string resref = objectId != NWScript.OBJECT_INVALID
            ? NWScript.GetResRef(objectId)
            : string.Empty;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["resref"] = resref
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get ResRef",
        Category = "Getters",
        Description = "Returns the blueprint ResRef of a game object.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin { Id = "object", Name = "Object", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "resref", Name = "ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
