using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Heals a creature by applying EffectHeal. Works in any encounter event.
/// </summary>
[GlyphNode]
public partial class HealCreatureExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.heal_creature";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput(Inputs.Creature);
        object? amountValue = await resolveInput(Inputs.Amount);

        uint creature = Convert.ToUInt32(creatureValue);
        int amount = Convert.ToInt32(amountValue);

        if (creature != NWScript.OBJECT_INVALID && amount > 0)
        {
            IntPtr effect = NWScript.EffectHeal(amount);
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, effect, creature);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("heal", null)
        ],
        DisplayName = "Heal Creature",
        Category = "Actions",
        Description = "Heals a creature for the specified amount of hit points.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        Parameters =
        [
            Pins.In("creature", "Creature", GlyphDataType.NwObject),
            Pins.In("amount", "Amount", GlyphDataType.Int, "10")
        ]
    };
}
