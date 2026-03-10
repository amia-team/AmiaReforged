using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Action node that plays a visual effect (VFX) on a target creature or object.
/// Supports both instant and duration-based effects using NWN VFX constants.
/// </summary>
public class PlayVfxExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.play_vfx";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? targetValue = await resolveInput("target");
        object? vfxIdValue = await resolveInput("vfx_id");
        object? durationValue = await resolveInput("duration");

        uint target = Convert.ToUInt32(targetValue);
        int vfxId = Convert.ToInt32(vfxIdValue);
        float duration = Convert.ToSingle(durationValue);

        if (target == NWScript.OBJECT_INVALID) return GlyphNodeResult.Continue("exec_out");

        if (duration <= 0f)
        {
            // Instant VFX
            NWScript.ApplyEffectToObject(
                NWScript.DURATION_TYPE_INSTANT,
                NWScript.EffectVisualEffect(vfxId),
                target);
        }
        else
        {
            // Duration-based VFX
            NWScript.ApplyEffectToObject(
                NWScript.DURATION_TYPE_TEMPORARY,
                NWScript.EffectVisualEffect(vfxId),
                target,
                duration);
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Play VFX",
        Category = "Actions",
        Description = "Plays a visual effect on a target. Use NWN VFX constant IDs. " +
                      "Duration 0 = instant effect, otherwise temporary for the given seconds. " +
                      "Common IDs: 16 (FNF_Fireball), 287 (DUR_GLOW_YELLOW), 45 (FNF_Sound_Burst).",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "target", Name = "Target", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "vfx_id", Name = "VFX ID", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "287" },
            new GlyphPin { Id = "duration", Name = "Duration (sec)", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Input, DefaultValue = "0" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
