using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Deals damage to a creature by applying EffectDamage. Works in any encounter event.
/// Supports common NWN damage types via string input.
/// </summary>
public class DamageCreatureExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.damage_creature";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? amountValue = await resolveInput("amount");
        object? damageTypeValue = await resolveInput("damage_type");

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

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Damage Creature",
        Category = "Actions",
        Description = "Deals damage of a specified type to a creature. " +
                      "Damage types: BLUDGEONING, PIERCING, SLASHING, FIRE, COLD, ACID, " +
                      "ELECTRICAL, DIVINE, NEGATIVE, POSITIVE, SONIC, MAGICAL.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "amount", Name = "Amount", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "10" },
            new GlyphPin { Id = "damage_type", Name = "Damage Type", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "MAGICAL" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
