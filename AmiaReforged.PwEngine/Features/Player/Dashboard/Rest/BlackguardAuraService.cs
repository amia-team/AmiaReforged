using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Rest;

/// <summary>
/// Handles Blackguard Aura of Despair functionality.
/// Starting at 3rd level, the blackguard radiates a malign aura that causes enemies
/// within 10 feet to take a -2 penalty on all saving throws.
///
/// Ported from: bg_despair.nss, bg_des_en.nss, bg_des_ex.nss
/// </summary>
[ServiceBinding(typeof(BlackguardAuraService))]
public class BlackguardAuraService
{
    private const string StackingPreventionVar = "CS_BGD_STACKING_AOD";
    private readonly ScriptHandleFactory _scriptHandleFactory;

    public BlackguardAuraService(ScriptHandleFactory scriptHandleFactory)
    {
        _scriptHandleFactory = scriptHandleFactory;
    }

    /// <summary>
    /// Creates the Aura of Despair effect for a Blackguard.
    /// </summary>
    public Effect CreateAuraOfDespair(NwCreature blackguard)
    {
        // Create script callback handles
        ScriptCallbackHandle enterHandle = _scriptHandleFactory.CreateUniqueHandler(info => OnEnterAura(info, blackguard));
        ScriptCallbackHandle exitHandle = _scriptHandleFactory.CreateUniqueHandler(info => OnExitAura(info, blackguard));

        // VFX 37 is AOE_PER_CUSTOM_AOE from the original script
        PersistentVfxTableEntry auraVfx = PersistentVfxType.PerCustomAoe!;

        // Create the AOE effect with callback handles
        Effect aura = Effect.AreaOfEffect(auraVfx, enterHandle, null, exitHandle);
        aura.Tag = "BlackguardAuraOfDespair";
        aura.SubType = EffectSubType.Extraordinary;

        return aura;
    }

    /// <summary>
    /// Called when a creature enters the Aura of Despair AOE.
    /// Applies -2 penalty to all saves for enemy creatures.
    /// </summary>
    private ScriptHandleResult OnEnterAura(CallInfo callInfo, NwCreature blackguard)
    {
        NwObject? entering = NWScript.GetEnteringObject().ToNwObject();
        if (entering is not NwCreature creature) return ScriptHandleResult.Handled;

        // Prevent stacking - only apply once per creature
        if (NWScript.GetLocalInt(creature, StackingPreventionVar) == NWScript.TRUE)
            return ScriptHandleResult.Handled;

        NWScript.SetLocalInt(creature, StackingPreventionVar, NWScript.TRUE);

        // Creature must be hostile to the Blackguard
        if (NWScript.GetIsEnemy(creature, blackguard) != NWScript.TRUE)
            return ScriptHandleResult.Handled;

        // Apply saving throw curse (permanent, untagged)
        IntPtr savePenalty = NWScript.EffectSavingThrowDecrease(NWScript.SAVING_THROW_ALL, 2);
        NWScript.ApplyEffectToObject(NWScript.DURATION_TYPE_PERMANENT, savePenalty, creature);


        return ScriptHandleResult.Handled;
    }

    /// <summary>
    /// Called when a creature exits the Aura of Despair AOE.
    /// Removes the -2 save penalty.
    /// </summary>
    private ScriptHandleResult OnExitAura(CallInfo callInfo, NwCreature blackguard)
    {
        NwObject? exiting = NWScript.GetExitingObject().ToNwObject();
        if (exiting is not NwCreature creature) return ScriptHandleResult.Handled;

        // Only process if the stacking prevention flag is set
        if (NWScript.GetLocalInt(creature, StackingPreventionVar) != NWScript.TRUE)
            return ScriptHandleResult.Handled;

        // Remove all saving throw decrease effects
        IntPtr effect = NWScript.GetFirstEffect(creature);
        while (NWScript.GetIsEffectValid(effect) == NWScript.TRUE)
        {
            if (NWScript.GetEffectType(effect) == NWScript.EFFECT_TYPE_SAVING_THROW_DECREASE)
            {
                NWScript.RemoveEffect(creature, effect);
                break;
            }
            effect = NWScript.GetNextEffect(creature);
        }

        // Clear the stacking prevention flag
        NWScript.DeleteLocalInt(creature, StackingPreventionVar);

        return ScriptHandleResult.Handled;
    }
}
