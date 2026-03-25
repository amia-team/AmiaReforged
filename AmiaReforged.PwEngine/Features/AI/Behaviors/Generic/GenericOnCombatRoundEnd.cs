using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI combat round end handler.
/// Ports logic from ds_ai2_endround.nss:
///
/// - Improved Grab counter reset (allows extra attacks each round)
/// - Living Bulwark check (redirects attack to guardian if target has "guarded" effect)
/// - Per-round random abilities (Per_Round system: configurable random spell/ability pool)
/// - OverrideAI check (skip if custom ability in progress)
/// - Darkness evasion (if blinded by Darkness and can't see target, move away)
/// - Standard PerformAction fallback for continued combat
/// </summary>
[ServiceBinding(typeof(IOnCombatRoundEndBehavior))]
public class GenericOnCombatRoundEnd : IOnCombatRoundEndBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiTargetingService _targetingService;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai2_endround";

    public GenericOnCombatRoundEnd(
        AiStateManager stateManager,
        AiTargetingService targetingService)
    {
        _stateManager = stateManager;
        _targetingService = targetingService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnCombatRoundEnd(CreatureEvents.OnCombatRoundEnd eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        // --- Improved Grab reset (ds_ai2_endround.nss lines 36-39) ---
        int impGrab = creature.GetObjectVariable<LocalVariableInt>("ImpGrab").Value;
        if (impGrab != 0)
        {
            creature.GetObjectVariable<LocalVariableInt>("ImpGrab").Value = 0;
        }

        // --- Living Bulwark check (ds_ai2_endround.nss lines 42-53) ---
        if (CheckLivingBulwark(creature))
        {
            return;
        }

        // --- Per-round random abilities (ds_ai2_endround.nss lines 56-113) ---
        if (ProcessPerRoundAbilities(creature))
        {
            return;
        }

        // --- OverrideAI check (custom ability in progress) ---
        int overrideAi = creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Value;
        if (overrideAi != 0)
        {
            return; // Custom spell/ability in progress, don't interrupt
        }

        // --- Darkness evasion (ds_ai2_endround.nss lines ~95-100) ---
        if (HandleDarknessEvasion(creature))
        {
            return;
        }

        // --- Standard combat continuation ---
        AiState? state = _stateManager.GetState(creature);
        if (state == null) return;

        NwGameObject? target = _targetingService.GetValidTarget(creature, state.CurrentTarget);
        if (target != null)
        {
            state.CurrentTarget = target;
            state.MarkActive();
            creature.ActionAttackTarget(target);
        }
    }

    /// <summary>
    /// Living Bulwark: if current target has a "guarded" effect tag, switch attack to the guardian.
    /// Ports ds_ai2_endround.nss lines 42-53.
    /// </summary>
    private bool CheckLivingBulwark(NwCreature creature)
    {
        NwGameObject? currentTarget = creature.AttackTarget;
        if (currentTarget == null) return false;

        // Check if target has the "guarded" local object (set by Living Bulwark ability)
        NwGameObject? guardian =
            currentTarget.GetObjectVariable<LocalVariableObject<NwGameObject>>("guarded").Value;

        if (guardian is not NwCreature guardianCreature || !guardianCreature.IsValid) return false;

        creature.ClearActionQueue();
        creature.ActionAttackTarget(guardianCreature);

        // Cooldown on guard switch message (6 seconds)
        int guardCd = creature.GetObjectVariable<LocalVariableInt>("guardCD").Value;
        if (guardCd == 0)
        {
            creature.GetObjectVariable<LocalVariableInt>("guardCD").Value = 1;
            NWScript.SpeakString($"*turns to attack {guardianCreature.Name}!*");
            NWScript.DelayCommand(6.0f,
                () => creature.GetObjectVariable<LocalVariableInt>("guardCD").Value = 0);
        }

        return true;
    }

    /// <summary>
    /// Per-round random abilities system from ds_ai2_endround.nss.
    /// Reads Per_Round (count of available abilities), picks one randomly each round.
    /// Abilities are stored as Random1, Random2, etc. with prefixes:
    /// - "spl_" or "nw_": spell cast (uses R{N}_SpellID and R{N}_Target)
    /// - "abl_": ability script execution
    /// 5.9s cooldown prevents double-firing within the same round.
    /// </summary>
    private bool ProcessPerRoundAbilities(NwCreature creature)
    {
        int perRoundCount = creature.GetObjectVariable<LocalVariableInt>("Per_Round").Value;
        if (perRoundCount <= 0) return false;

        // Cooldown check (5.9s between uses)
        int enRoundUsed = creature.GetObjectVariable<LocalVariableInt>("EnRoundUsed").Value;
        if (enRoundUsed == 1) return false;

        // Pick a random ability from the pool
        int randomIndex = Random.Shared.Next(perRoundCount);
        string abilityRef =
            creature.GetObjectVariable<LocalVariableString>($"Random{randomIndex}").Value ?? "";

        if (string.IsNullOrEmpty(abilityRef)) return false;

        // Set cooldown
        creature.GetObjectVariable<LocalVariableInt>("EnRoundUsed").Value = 1;
        NWScript.DelayCommand(5.9f,
            () => creature.GetObjectVariable<LocalVariableInt>("EnRoundUsed").Value = 0);

        // Parse ability type by prefix
        if (abilityRef.StartsWith("spl_") || abilityRef.StartsWith("nw_"))
        {
            // Spell cast
            int spellId =
                creature.GetObjectVariable<LocalVariableInt>($"R{randomIndex}_SpellID").Value;
            int targetType =
                creature.GetObjectVariable<LocalVariableInt>($"R{randomIndex}_Target").Value;

            if (spellId != 0)
            {
                NwGameObject? spellTarget = ResolveTarget(creature, targetType);
                if (spellTarget != null)
                {
                    creature.ClearActionQueue();
                    NWScript.ActionCastSpellAtObject(spellId, spellTarget, (int)MetaMagic.Any, 1, 0, 0, 1);
                    return true;
                }
            }
        }
        else if (abilityRef.StartsWith("abl_"))
        {
            // Script execution
            NWScript.ExecuteScript(abilityRef, creature);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Darkness evasion: if creature has Darkness effect and can't see target, move away 7m.
    /// Ports ds_ai2_endround.nss darkness handling block.
    /// </summary>
    private bool HandleDarknessEvasion(NwCreature creature)
    {
        bool hasDarkness = creature.ActiveEffects.Any(e => e.EffectType == EffectType.Darkness);
        if (!hasDarkness) return false;

        NwGameObject? target = creature.AttackTarget;
        if (target == null) return false;

        // If can't see target while in darkness, move away
        if (target is NwCreature targetCreature && !creature.IsCreatureSeen(targetCreature))
        {
            creature.ClearActionQueue();
            creature.ActionMoveAwayFrom(target, true, 7.0f);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves a target by type for per-round abilities.
    /// Matches FindNPCSpellTarget() routing.
    /// </summary>
    private NwGameObject? ResolveTarget(NwCreature creature, int targetType)
    {
        return targetType switch
        {
            1 => creature,
            2 => creature.AttackTarget,
            3 => _targetingService.FindNearestEnemy(creature),
            4 => _targetingService.FindNearestEnemy(creature),
            5 => creature.GetNearestCreatures(CreatureTypeFilter.Reputation(ReputationType.Friend))
                .FirstOrDefault(),
            _ => creature
        };
    }
}
