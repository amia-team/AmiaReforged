using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Destroys a creature after an optional delay. Useful for custom despawn logic.
/// </summary>
public class DespawnCreatureExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.despawn_creature";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? delayValue = await resolveInput("delay_seconds");

        uint creature = Convert.ToUInt32(creatureValue);
        float delay = Convert.ToSingle(delayValue);

        if (creature != NWScript.OBJECT_INVALID)
        {
            if (delay <= 0)
            {
                NWScript.DestroyObject(creature);
            }
            else
            {
                NWScript.DelayCommand(delay, () => NWScript.DestroyObject(creature));
            }
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Despawn Creature",
        Category = "Actions",
        Description = "Destroys a creature, optionally after a delay.",
        ColorClass = "node-action",
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "delay_seconds", Name = "Delay (sec)", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
