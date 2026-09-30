using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the current HP of a creature. Pure data node — no execution flow.
/// </summary>
[GlyphNode]
public sealed partial class GetCreatureHPExecutor : GlyphPureNode
{
    public const string NodeTypeId = "getter.creature_hp";

    public override string TypeId => NodeTypeId;

    protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx)
    {
        uint creature = await cx.InObject(Inputs.Creature);

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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("creature.hp", "current_hp", null, PropertyAliases: [new("creature.hp", "creature.hp", "creature")]),
            new("creature.max_hp", "max_hp", null, PropertyAliases: [new("creature.max_hp", "creature.max_hp", "creature")])
        ],
        DisplayName = "Get Creature HP",
        Category = "Getters",
        Description = "Returns the current and maximum hit points of a creature.",
        ColorClass = "node-getter",
        Parameters =
        [
            Pins.InObject("creature", "Creature"),
        ],
        Results =
        [
            Pins.Out("current_hp", "Current HP", GlyphDataType.Int),
            Pins.Out("max_hp", "Max HP", GlyphDataType.Int),
        ]
    };
}
