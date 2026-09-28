using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns whether the supplied NWN object is a player character, using the established
/// <c>NWScript.GetIsPC</c> semantic. Pure query node — no execution flow and no side effects.
/// Returns <c>false</c> for invalid or unresolvable objects.
/// </summary>
public class IsPlayerExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.is_player";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? objectValue = await resolveInput("object");
        uint objectId = ConvertId(objectValue);

        bool isPlayer = objectId != 0 && objectId != NWScript.OBJECT_INVALID
            ? NWScript.GetIsPC(objectId) == NWScript.TRUE
            : false;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["result"] = isPlayer
        });
    }

    private static uint ConvertId(object? value) => value is null ? 0u : Convert.ToUInt32(value);

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Is Player",
        Category = "Getters",
        Description = "Returns true when the object is a player character (NWScript.GetIsPC). " +
                      "Returns false for invalid or unresolvable objects.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin
            {
                Id = "object", Name = "Object", DataType = GlyphDataType.NwObject,
                Direction = GlyphPinDirection.Input
            }
        ],
        OutputPins =
        [
            new GlyphPin
            {
                Id = "result", Name = "Result", DataType = GlyphDataType.Bool,
                Direction = GlyphPinDirection.Output
            }
        ]
    };
}
