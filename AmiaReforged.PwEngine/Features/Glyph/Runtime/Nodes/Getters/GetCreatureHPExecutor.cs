using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the current HP of a creature. Pure data node — no execution flow.
/// </summary>
public sealed class GetCreatureHPExecutor : GlyphPureNode
{
    public const string NodeTypeId = "getter.creature_hp";

    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        uint creature = await cx.InObject("creature");

        int hp = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetCurrentHitPoints(creature)
            : 0;

        int maxHp = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetMaxHitPoints(creature)
            : 0;

        return new Dictionary<string, object?>
        {
            ["current_hp"] = hp,
            ["max_hp"] = maxHp
        };
    }

    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature HP",
        Category = "Getters",
        Description = "Returns the current and maximum hit points of a creature.",
        ColorClass = "node-getter",
        InputPins =
        [
            Pins.InObject("creature", "Creature"),
        ],
        OutputPins =
        [
            Pins.Out("current_hp", "Current HP", GlyphDataType.Int),
            Pins.Out("max_hp", "Max HP", GlyphDataType.Int),
        ]
    };
}
