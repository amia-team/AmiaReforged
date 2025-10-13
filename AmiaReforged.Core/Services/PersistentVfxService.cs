using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(PersistentVfxService))]

public class PersistentVfxService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string PcKeyTag = "ds_pckey";
    private const string PersistentVfxPrefix = "persistentvfx";
    private const string FloatSuffix = "float";
    private const string TranslateSuffix = "translate";
    private const string RotateSuffix = "rotate";
    private const string TagSuffix = "tag";


    public PersistentVfxService(EventService eventService)
    {
        NwModule.Instance.OnEffectApply += StorePersistentVfx;
        NwModule.Instance.OnEffectRemove += RemoveStoredPersistentVfx;
        eventService.SubscribeAll<OnLoadCharacterFinish, OnLoadCharacterFinish.Factory>(ApplyPersistentVfx, EventCallbackType.After);

        Log.Info("Persistent Vfx Service initialized.");
    }

    /// <summary>
    ///     When a permanent-duration unyielding vfx is applied, stores it in the player's PCKey for later application
    /// </summary>
    private void StorePersistentVfx(OnEffectApply obj)
    {
        // Creature must be a normal player charcter
        if (obj.Object is not NwCreature playerCharacter) return;
        if (!playerCharacter.IsLoginPlayerCharacter) return;
        if (playerCharacter.IsDMPossessed) return;

        // To be persistentvfx, effect must be a visual effect, subtype unyielding, and permanent duration
        if (obj.Effect.EffectType is not EffectType.VisualEffect) return;
        if (obj.Effect.SubType is not EffectSubType.Unyielding) return;
        if (obj.Effect.DurationType is not EffectDuration.Permanent) return;

        // Declare variables
        Effect persistentVfx = obj.Effect;
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == PcKeyTag);
        int vfxId = persistentVfx.IntParams[0];
        float vfxScale = persistentVfx.FloatParams[0];
        Vector3 vfxTranslate = persistentVfx.VectorParams[0];
        Vector3 vfxRotate = persistentVfx.VectorParams[1];
        string? vfxTag = persistentVfx.Tag;

        // Return if the data is already stored
        if (pcKey.GetObjectVariable<LocalVariableInt>(PersistentVfxPrefix+vfxId).HasValue) return;

        // Store persistentvfx data for later calling
        pcKey.GetObjectVariable<LocalVariableInt>(PersistentVfxPrefix+vfxId).Value = vfxId;
        pcKey.GetObjectVariable<LocalVariableFloat>(PersistentVfxPrefix+vfxId+FloatSuffix).Value = vfxScale;
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>(PersistentVfxPrefix+vfxId+TranslateSuffix).Value = vfxTranslate;
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>(PersistentVfxPrefix+vfxId+RotateSuffix).Value = vfxRotate;
        if (vfxTag != null)
            pcKey.GetObjectVariable<LocalVariableString>(PersistentVfxPrefix+vfxId+TagSuffix).Value = vfxTag;
    }

    /// <summary>
    ///     Removes persistent vfx variables when it's removed with RemoveEffect
    /// </summary>
    private void RemoveStoredPersistentVfx(OnEffectRemove obj)
    {
        // Creature must be a normal player character
        if (obj.Object is not NwCreature playerCharacter) return;
        if (!playerCharacter.IsLoginPlayerCharacter) return;
        if (playerCharacter.IsDMPossessed) return;

        // To be persistentvfx, effect must be a visual effect, subtype unyielding, and permanent duration
        if (obj.Effect.EffectType is not EffectType.VisualEffect) return;
        if (obj.Effect.SubType is not EffectSubType.Unyielding) return;
        if (obj.Effect.DurationType is not EffectDuration.Permanent) return;

        // Declare variables
        Effect persistentVfx = obj.Effect;
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == PcKeyTag);
        int vfxId = persistentVfx.IntParams[0];

        // Remove persistentvfx data
        pcKey.GetObjectVariable<LocalVariableInt>(PersistentVfxPrefix+vfxId).Delete();
        pcKey.GetObjectVariable<LocalVariableFloat>(PersistentVfxPrefix+vfxId+FloatSuffix).Delete();
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>(PersistentVfxPrefix+vfxId+TranslateSuffix).Delete();
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>(PersistentVfxPrefix+vfxId+RotateSuffix).Delete();
        pcKey.GetObjectVariable<LocalVariableString>(PersistentVfxPrefix+vfxId+TagSuffix).Delete();
    }

    /// <summary>
    ///     Gets the persistent vfx data and reapplies them on loading the character
    /// </summary>
    private void ApplyPersistentVfx(OnLoadCharacterFinish obj)
    {
        // Creature must be a normal player character
        if (obj.Player.ControlledCreature is null) return;
        NwCreature playerCharacter = obj.Player.ControlledCreature;

        if (!playerCharacter.IsPlayerControlled) return;
        if (playerCharacter.IsDMPossessed) return;

        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == PcKeyTag);

        foreach (LocalVariableInt varInt in pcKey.LocalVariables.OfType<LocalVariableInt>())
        {
            if (!varInt.Name.Contains(PersistentVfxPrefix)) continue;

            int vfxId = varInt.Value;

            // skip duplicate visuals for persistent vfxs
            if (playerCharacter.ActiveEffects.Any(effect =>
                    effect.IntParams[0] == vfxId &&
                    effect is { SubType: EffectSubType.Unyielding, DurationType: EffectDuration.Permanent })) continue;

            // Otherwise, continue to set the persistent vfx
            float vfxScale = pcKey.GetObjectVariable<LocalVariableFloat>(PersistentVfxPrefix+vfxId+FloatSuffix).Value;
            // vector translate variable
            Vector3 vfxTranslate = pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>
                (PersistentVfxPrefix+vfxId+TranslateSuffix).Value;
            // vector rotate variable
            Vector3 vfxRotate = pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>
                (PersistentVfxPrefix+vfxId+RotateSuffix).Value;

            // Apply persistent vfx
            Effect persistentVfx = Effect.VisualEffect(NwGameTables.VisualEffectTable[vfxId], false, vfxScale, vfxTranslate,  vfxRotate);
            persistentVfx.SubType = EffectSubType.Unyielding;

            if (pcKey.GetObjectVariable<LocalVariableString>(PersistentVfxPrefix+vfxId+TagSuffix).HasValue)
                persistentVfx.Tag = pcKey.GetObjectVariable<LocalVariableString>(PersistentVfxPrefix+vfxId+TagSuffix).Value;

            playerCharacter.ApplyEffect(EffectDuration.Permanent, persistentVfx);
        }
    }
}
