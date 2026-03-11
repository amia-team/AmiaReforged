using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Getter node that returns the NWN object ID of the player creature that triggered the encounter.
/// Useful when the triggering player reference is needed deeper in a subgraph where the event
/// entry node's output pin isn't directly wirable.
/// </summary>
public class GetTriggeringPlayerExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.triggering_player";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["player"] = context.TriggeringPlayer
        }));
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Triggering Player",
        Category = "Getters",
        Description = "Returns the player creature that triggered the encounter (entered the trigger or area). " +
                      "Can be passed to Send Message, Apply Effect, and other action nodes.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Encounter,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin
            {
                Id = "player",
                Name = "Player Creature",
                DataType = GlyphDataType.NwObject,
                Direction = GlyphPinDirection.Output
            }
        ]
    };
}
