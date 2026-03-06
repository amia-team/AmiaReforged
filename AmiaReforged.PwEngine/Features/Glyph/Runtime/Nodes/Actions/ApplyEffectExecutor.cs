using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Applies a visual/mechanical NWN effect to a creature.
/// Supports common effect types used in encounter design.
/// </summary>
public class ApplyEffectExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.apply_effect";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? effectTypeValue = await resolveInput("effect_type");
        object? magnitudeValue = await resolveInput("magnitude");
        object? durationValue = await resolveInput("duration");

        uint creature = Convert.ToUInt32(creatureValue);
        string effectType = effectTypeValue?.ToString() ?? "TempHP";
        int magnitude = Convert.ToInt32(magnitudeValue);
        float duration = Convert.ToSingle(durationValue);

        if (creature == NWScript.OBJECT_INVALID) return GlyphNodeResult.Continue("exec_out");

        IntPtr effect = CreateNwnEffect(effectType, magnitude);
        if (effect == IntPtr.Zero) return GlyphNodeResult.Continue("exec_out");

        int durationType = duration <= 0
            ? NWScript.DURATION_TYPE_PERMANENT
            : NWScript.DURATION_TYPE_TEMPORARY;

        NWScript.ApplyEffectToObject(durationType, effect, creature, duration);

        return GlyphNodeResult.Continue("exec_out");
    }

    private static IntPtr CreateNwnEffect(string effectType, int magnitude)
    {
        return effectType.ToUpperInvariant() switch
        {
            "TEMPHP" => NWScript.EffectTemporaryHitpoints(magnitude),
            "AC" => NWScript.EffectACIncrease(magnitude),
            "DAMAGE_RESISTANCE" => NWScript.EffectDamageResistance(NWScript.DAMAGE_TYPE_SLASHING, magnitude),
            "ATTACK" => NWScript.EffectAttackIncrease(magnitude),
            "DAMAGE" => NWScript.EffectDamageIncrease(magnitude, NWScript.DAMAGE_TYPE_MAGICAL),
            "SAVING_THROW" => NWScript.EffectSavingThrowIncrease(NWScript.SAVING_THROW_ALL, magnitude),
            "CONCEALMENT" => NWScript.EffectConcealment(Math.Clamp(magnitude, 1, 100)),
            "HASTE" => NWScript.EffectHaste(),
            "SLOW" => NWScript.EffectSlow(),
            "REGENERATE" => NWScript.EffectRegenerate(magnitude, 6.0f),
            "VISUAL_FIRE" => NWScript.EffectVisualEffect(NWScript.VFX_DUR_AURA_FIRE),
            "VISUAL_COLD" => NWScript.EffectVisualEffect(NWScript.VFX_DUR_AURA_COLD),
            "VISUAL_EVIL" => NWScript.EffectVisualEffect(NWScript.VFX_DUR_AURA_EVIL),
            _ => IntPtr.Zero
        };
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Apply Effect",
        Category = "Actions",
        Description = "Applies an NWN effect to a creature. Supports TempHP, AC, Attack, Damage, " +
                      "Concealment, Haste, Slow, Regenerate, and visual effects.",
        ColorClass = "node-action",
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "effect_type", Name = "Effect Type", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "TempHP" },
            new GlyphPin { Id = "magnitude", Name = "Magnitude", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "10" },
            new GlyphPin { Id = "duration", Name = "Duration (sec)", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
