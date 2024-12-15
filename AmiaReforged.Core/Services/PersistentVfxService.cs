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
        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == "ds_pckey");
        int vfxId = persistentVfx.IntParams[0];

        // Remove persistentvfx data
        pcKey.GetObjectVariable<LocalVariableInt>("persistentvfx"+vfxId).Delete();
        pcKey.GetObjectVariable<LocalVariableFloat>("persistentvfx"+vfxId+"float").Delete();
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"translate").Delete();
        pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>("persistentvfx"+vfxId+"rotate").Delete();
    }

    /// <summary>
    ///     Gets the persistent vfx data and reapplies them on loading the character
    /// </summary>
    private async void ApplyPersistentVfx(OnLoadCharacterFinish obj)
    {
        // Creature must be a normal player character
        if (obj.Player.ControlledCreature is not NwCreature playerCharacter) return;
        if (!playerCharacter.IsPlayerControlled) return;
        if (playerCharacter.IsDMPossessed) return;

        NwItem pcKey = playerCharacter.Inventory.Items.First(item => item.Tag == "ds_pckey");

        await NwTask.Delay(TimeSpan.FromSeconds(3));

        if (!playerCharacter.IsValid) return;
        
        foreach (LocalVariableInt varInt in pcKey.LocalVariables.OfType<LocalVariableInt>())
        {
            if (!varInt.Name.Contains("persistentvfx")) return;
            
            int vfxId = varInt.Value;

            // skip duplicate visuals for persistent vfxs
            if (playerCharacter.ActiveEffects.Any(effect => effect.IntParams[0] == vfxId 
                && effect.DurationType == EffectDuration.Permanent && effect.SubType ==
                EffectSubType.Unyielding)) continue;

            // Otherwise, continue to set the persistent vfx
            float vfxScale = pcKey.GetObjectVariable<LocalVariableFloat>("persistentvfx"+vfxId+"float");
            // vector translate variable
            Vector3 vfxTranslate = pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>  
            ("persistentvfx"+vfxId+"translate");
            // vector rotate variable
            Vector3 vfxRotate = pcKey.GetObjectVariable<LocalVariableStruct<Vector3>>
            ("persistentvfx"+vfxId+"rotate");
            
            // Apply persistent vfx
            playerCharacter.ApplyEffect(EffectDuration.Permanent, 
            Effect.VisualEffect(NwGameTables.VisualEffectTable[vfxId], false, vfxScale, vfxTranslate,  
            vfxRotate));
        }
    }
}