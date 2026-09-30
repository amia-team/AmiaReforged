using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the tag of any NWN game object. Pure data node — no execution flow.
/// </summary>
[GlyphNode]
public partial class GetTagExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.tag";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectValue = await resolveInput("object");
        uint objectId = Nwn.GlyphNwnValue.NormalizeObject(Convert.ToUInt32(objectValue));

        string tag = objectId != NWScript.OBJECT_INVALID
            ? NWScript.GetTag(objectId)
            : string.Empty;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["tag"] = tag
        });
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId, DisplayName = "GetTagExecutor", Category = "NWN / Compatibility",
        Description = "NWScript GetTag with the established runtime pin contract.",
        Source = "NWScript.GetTag", Backend = "NWScript adapter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Parameters = [Pins.InObject("object", "Object")], Results = [Pins.Out("tag", "Tag", GlyphDataType.String)],
        Exports = [new("nwn.get_tag", "tag", ReceiverMethods: ["get_tag"])]
    };
}
