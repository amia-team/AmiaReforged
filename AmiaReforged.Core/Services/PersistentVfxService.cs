using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.DMS.Services;

[ServiceBinding(typeof(PersistentVfxService))]

public class PersistentVfxService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    
    public PersistentVfxService()
    {
        NwModule.Instance.OnEffectApply += StorePersistentVfx;
        /* NwModule.Instance.OnEffectRemove += RemoveStoredPersistentVfx; */
        NwModule.Instance.OnClientEnter += ApplyPersistentVfx;
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
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == "ds_pckey");
        int vfxId = persistentVfx.IntParams[0];
        float vfxScale = persistentVfx.FloatParams[0];
        Vector3 vfxTranslate = persistentVfx.VectorParams[0];
        Vector3 vfxRotate = persistentVfx.VectorParams[1];

        // Return if the data is already stored
        if (pcKey.GetObjectVariable<LocalVariableInt>("persistentvfx"+vfxId).HasValue) return;

        // Store persistentvfx data for later calling
        pcKey.GetObjectVariable<LocalVariableInt>("persistentvfx"+vfxId).Value = vfxId;
        pcKey.GetObjectVariable<LocalVariableFloat>("persistentvfx"+vfxId+"float").Value = vfxScale;
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"translate").Value = vfxTranslate;
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"rotate").Value = vfxTranslate;
    }

    /* /// <summary>
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
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == "ds_pckey");
        int vfxId = persistentVfx.IntParams[0];

        // Remove persistentvfx data
        pcKey.GetObjectVariable<LocalVariableInt>("persistentvfx"+vfxId).Delete();
        pcKey.GetObjectVariable<LocalVariableFloat>("persistentvfx"+vfxId+"float").Delete();
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"translate").Delete();
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"rotate").Delete();
    } */

    /// <summary>
    ///     Gets the persistent vfx data and reapplies them on client enter
    /// </summary>
    private void ApplyPersistentVfx(ModuleEvents.OnClientEnter obj)
    {
        // Creature must be a normal player character
        if (obj.Player.ControlledCreature is not NwCreature playerCharacter) return;
        if (!playerCharacter.IsPlayerControlled) return;
        if (playerCharacter.IsDMPossessed) return;

        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == "ds_pckey");

        // Loop for each unique persistent vfx stored in the pckey and reapply them
        foreach (LocalVariableInt varInt in pcKey.LocalVariables.Cast<LocalVariableInt>())
        {
            if (varInt.Name.Contains("persistentvfx")) 
            {
                int vfxId = varInt.Value;
                float vfxScale = pcKey.GetObjectVariable<LocalVariableFloat>("persistentvfx"+vfxId+"float");
                Vector3 vfxTranslate = pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"translate");
                Vector3 vfxRotate = pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"rotate");
                playerCharacter.ApplyEffect(EffectDuration.Permanent, 
                    Effect.VisualEffect(NwGameTables.VisualEffectTable[vfxId], false, vfxScale, vfxTranslate, vfxRotate));
            }
        }
    }
}