using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Action node that plays a visual effect (VFX) on a target creature or object.
/// Supports both instant and duration-based effects using NWN VFX constants.
/// </summary>
[GlyphNode]
public partial class PlayVfxExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.play_vfx";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? targetValue = await resolveInput(Inputs.Target);
        object? vfxIdValue = await resolveInput(Inputs.VfxId);
        object? durationValue = await resolveInput(Inputs.Duration);

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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("play_vfx", null)
        ],
        DisplayName = "Play VFX",
        Category = "Actions",
        Description = "Plays a visual effect on a target. Use NWN VFX constant IDs. " +
                      "Duration 0 = instant effect, otherwise temporary for the given seconds. " +
                      "Common IDs: 16 (FNF_Fireball), 287 (DUR_GLOW_YELLOW), 45 (FNF_Sound_Burst).",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("target", "Target", GlyphDataType.NwObject),
            Pins.In("vfx_id", "VFX ID", GlyphDataType.Int, "287"),
            Pins.In("duration", "Duration (sec)", GlyphDataType.Float, "0")
        ]
    };
}
