using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets the current HP of a creature. Pure data node — no execution flow.
/// </summary>
public class GetCreatureHPExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.creature_hp";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        uint creature = Convert.ToUInt32(creatureValue);

        int hp = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetCurrentHitPoints(creature)
            : 0;

        int maxHp = creature != NWScript.OBJECT_INVALID
            ? NWScript.GetMaxHitPoints(creature)
            : 0;

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["current_hp"] = hp,
            ["max_hp"] = maxHp
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature HP",
        Category = "Getters",
        Description = "Returns the current and maximum hit points of a creature.",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "current_hp", Name = "Current HP", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "max_hp", Name = "Max HP", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
