using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the blueprint ResRef of any NWN game object. Pure data node — no execution flow.
/// Unlike <see cref="GetCreatureResRefExecutor"/> which is creature-specific, this works
/// with any object type (placeables, doors, items, etc.).
/// </summary>
[GlyphNode]
public partial class GetObjectResRefExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.object_resref";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectValue = await resolveInput("object");
        uint objectId = Nwn.GlyphNwnValue.NormalizeObject(Convert.ToUInt32(objectValue));

        string resref = objectId != NWScript.OBJECT_INVALID
            ? NWScript.GetResRef(objectId)
            : string.Empty;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["resref"] = resref
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId, DisplayName = "GetObjectResRefExecutor", Category = "NWN / Compatibility",
        Description = "NWScript GetResRef with the established runtime pin contract.",
        Source = "NWScript.GetResRef", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("object", "Object")], Results = [Pins.Out("resref", "ResRef", GlyphDataType.String)],
        Exports = [new("nwn.get_resref", "resref"), new("nwn.get_res_ref", "resref")]
    };
}
