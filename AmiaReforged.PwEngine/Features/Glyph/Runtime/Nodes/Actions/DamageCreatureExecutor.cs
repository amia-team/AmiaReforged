using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Deals damage to a creature by applying EffectDamage. Works in any encounter event.
/// Supports common NWN damage types via string input.
/// </summary>
[GlyphNode]
public partial class DamageCreatureExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.damage_creature";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput(Inputs.Creature);
        object? amountValue = await resolveInput(Inputs.Amount);
        object? damageTypeValue = await resolveInput(Inputs.DamageType);

        uint creature = Convert.ToUInt32(creatureValue);
        int amount = Convert.ToInt32(amountValue);
        string damageTypeStr = damageTypeValue?.ToString()?.ToUpperInvariant() ?? "MAGICAL";

        int damageType = damageTypeStr switch
        {
            "BLUDGEONING" => NWScript.DAMAGE_TYPE_BLUDGEONING,
            "PIERCING" => NWScript.DAMAGE_TYPE_PIERCING,
            "SLASHING" => NWScript.DAMAGE_TYPE_SLASHING,
            "FIRE" => NWScript.DAMAGE_TYPE_FIRE,
            "COLD" => NWScript.DAMAGE_TYPE_COLD,
            "ACID" => NWScript.DAMAGE_TYPE_ACID,
            "ELECTRICAL" => NWScript.DAMAGE_TYPE_ELECTRICAL,
            "DIVINE" => NWScript.DAMAGE_TYPE_DIVINE,
            "NEGATIVE" => NWScript.DAMAGE_TYPE_NEGATIVE,
            "POSITIVE" => NWScript.DAMAGE_TYPE_POSITIVE,
            "SONIC" => NWScript.DAMAGE_TYPE_SONIC,
            "MAGICAL" => NWScript.DAMAGE_TYPE_MAGICAL,
            _ => NWScript.DAMAGE_TYPE_MAGICAL
        };

        if (creature != NWScript.OBJECT_INVALID && amount > 0)
        {
            IntPtr effect = NWScript.EffectDamage(amount, damageType);
            NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_INSTANT, effect, creature);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("damage", null)
        ],
        DisplayName = "Damage Creature",
        Category = "Actions",
        Description = "Deals damage of a specified type to a creature. " +
                      "Damage types: BLUDGEONING, PIERCING, SLASHING, FIRE, COLD, ACID, " +
                      "ELECTRICAL, DIVINE, NEGATIVE, POSITIVE, SONIC, MAGICAL.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        Parameters =
        [
            Pins.In("creature", "Creature", GlyphDataType.NwObject),
            Pins.In("amount", "Amount", GlyphDataType.Int, "10"),
            Pins.In("damage_type", "Damage Type", GlyphDataType.String, "MAGICAL")
        ]
    };
}
