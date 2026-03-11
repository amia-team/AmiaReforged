using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Heals a creature by applying EffectHeal. Works in any encounter event.
/// </summary>
public class HealCreatureExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.heal_creature";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? amountValue = await resolveInput("amount");

        uint creature = Convert.ToUInt32(creatureValue);
        int amount = Convert.ToInt32(amountValue);

        if (creature != NWScript.OBJECT_INVALID && amount > 0)
        {
            IntPtr effect = NWScript.EffectHeal(amount);
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, effect, creature);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Heal Creature",
        Category = "Actions",
        Description = "Heals a creature for the specified amount of hit points.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "amount", Name = "Amount", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "10" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
