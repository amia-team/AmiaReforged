using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Module.DeveloperTools;

/// <summary>
/// Applies persistent visual effects to placeables on module load.
/// Searches for placeables with tag "obj_vfx_persist" and applies VFX based on local variables.
///
/// Local Variables:
/// - "ds_ai_vfx_1" through "ds_ai_vfx_10" (INT): VFX IDs to apply as unyielding permanent effects.
/// </summary>
[ServiceBinding(typeof(PersistentPlaceableVfxService))]
public class PersistentPlaceableVfxService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string VfxPlaceableTag = "obj_vfx_persist";
    private const string VfxVariablePrefix = "ds_ai_vfx_";
    private const int MaxVfxSlots = 10;

    public PersistentPlaceableVfxService()
    {
        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        try
        {
            ApplyVfxToPlaceables();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply persistent VFX to placeables during module load.");
        }
    }

    private void ApplyVfxToPlaceables()
    {
        List<NwPlaceable> vfxPlaceables = NwObject.FindObjectsWithTag<NwPlaceable>(VfxPlaceableTag).ToList();
        int appliedCount = 0;

        foreach (NwPlaceable placeable in vfxPlaceables)
        {
            if (!placeable.IsValid) continue;

            // Check all VFX slots (1-10)
            for (int slot = 1; slot <= MaxVfxSlots; slot++)
            {
                string variableName = $"{VfxVariablePrefix}{slot}";
                int vfxId = placeable.GetObjectVariable<LocalVariableInt>(variableName).Value;

                // Only apply if the variable has a value (non-zero)
                if (vfxId == 0) continue;

                try
                {
                    Effect vfxEffect = Effect.VisualEffect((VfxType)vfxId);
                    vfxEffect.SubType = EffectSubType.Unyielding;
                    placeable.ApplyEffect(EffectDuration.Permanent, vfxEffect);
                    appliedCount++;
                }
                catch (Exception ex)
                {
                    Log.Warn(ex, "Failed to apply VFX ID {VfxId} (slot {Slot}) to placeable {Tag} in area {Area}.",
                        vfxId, slot, placeable.Tag, placeable.Area?.Name ?? "Unknown");
                }
            }
        }

        if (appliedCount > 0)
        {
            Log.Info("Applied {Count} persistent VFX effect(s) to placeables.", appliedCount);
        }
    }
}

