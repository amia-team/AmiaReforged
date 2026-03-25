using AmiaReforged.PwEngine.Features.AI.Core.Models;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI damaged handler that manages target switching, flee behavior, and HP-triggered abilities.
/// Ports logic from ds_ai2_damaged.nss:
///
/// - Flee logic with archetype modifier: casters/ranged get -2 to d10 roll, making them
///   more likely to flee when damaged at close range
/// - Target switching: chance to switch to damager if different from current target
/// - HP-percentage triggers (SpellID_N and AbilityID_N): one-shot custom spells/scripts
///   that fire when creature HP drops to N%
/// - OverrideAI pattern: temporarily disables AI for 1-4s during custom ability execution
/// - DeathEffect scripts: stores killer and executes named script on death
/// </summary>
[ServiceBinding(typeof(IOnDamagedBehavior))]
public class GenericAiDamaged : IOnDamagedBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiArchetypeService _archetypeService;
    private readonly AiTargetingService _targetingService;
    private readonly bool _isEnabled;

    /// <summary>
    /// BREAKCOMBAT constant from ds_ai_include.nss (chance to switch targets).
    /// Legacy: (d100 - 20) < 25, which is ~24% chance.
    /// </summary>
    private const int BreakCombat = 25;

    public string ScriptName => "ds_ai_damaged";

    public GenericAiDamaged(
        AiStateManager stateManager,
        AiArchetypeService archetypeService,
        AiTargetingService targetingService)
    {
        _stateManager = stateManager;
        _archetypeService = archetypeService;
        _targetingService = targetingService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnDamaged(CreatureEvents.OnDamaged eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        NwGameObject damager = eventData.Damager;
        if (damager is not NwCreature damagerCreature) return;

        AiState? state = _stateManager.GetState(creature);
        if (state == null) return;

        // Track last damager
        state.LastDamager = damagerCreature;

        // --- Flee/switch logic with archetype modifier ---
        int reaction = GetReaction(creature, damagerCreature, state);

        if (reaction == 2) // Flee
        {
            float distance = creature.Distance(damagerCreature);
            if (distance < 5.0f)
            {
                creature.ClearActionQueue();
                creature.ActionMoveAwayFrom(damagerCreature, true, 10.0f);
                state.CurrentTarget = damagerCreature;
            }
        }
        else if (reaction == 1) // Switch target
        {
            if (state.CurrentTarget != damagerCreature)
            {
                if (NWScript.GetObjectSeen(damagerCreature, creature) == 1)
                {
                    int breakRoll = Random.Shared.Next(1, 101) - 20;
                    if (breakRoll < BreakCombat)
                    {
                        state.CurrentTarget = damagerCreature;
                    }
                }
            }
        }

        // --- HP-percentage triggers (ds_ai2_damaged.nss lines 53-91) ---
        ProcessHpPercentageTriggers(creature, damagerCreature);
    }

    /// <summary>
    /// Gets combat reaction with archetype-weighted flee bias.
    /// Ports GetReaction() from ds_ai_include.nss (lines 1767-1823):
    /// - Casters/ranged get -2 modifier to the d10 roll
    /// - (d10 + 2) < archetypeValue → flee (for casters: d10 range is effectively shifted)
    /// Returns: 0 = no change, 1 = switch target, 2 = flee
    /// </summary>
    private int GetReaction(NwCreature creature, NwCreature damager, AiState state)
    {
        if (!damager.IsValid) return 0;

        int roll = Random.Shared.Next(1, 11); // d10

        // Archetype modifier: casters and ranged get -2
        string archetype = state.ArchetypeId ?? "melee";
        if (archetype is "caster" or "ranged")
        {
            roll -= 2;
        }

        // Get archetype numeric value for flee threshold comparison
        int archetypeValue = _archetypeService.GetArchetypeValue(creature);

        // Caster flee: (d10 + 2) < archetypeValue → flee at close range
        if ((roll + 2) < archetypeValue)
        {
            return 2; // Flee
        }

        // Pet-to-PC preference: 50% chance to prefer attacking the master
        NwCreature? master = damager.Master;
        if (master != null && master.IsPlayerControlled)
        {
            if (Random.Shared.Next(2) == 0)
            {
                return 1; // Switch to the PC
            }
        }

        return 0;
    }

    /// <summary>
    /// Processes HP-percentage trigger system from ds_ai2_damaged.nss.
    /// Checks SpellID_N and AbilityID_N local variables (N = 1-99):
    /// - SpellID_N: cheat-casts spell when HP ≤ N%, one-shot (deleted after use)
    /// - AbilityID_N: executes named script when HP ≤ N%, one-shot
    /// - Target_N: determines target routing (1=self, 2=current, 3=random enemy, etc.)
    /// - OverrideAI: temporarily disables AI for 4s during ability execution
    /// </summary>
    private void ProcessHpPercentageTriggers(NwCreature creature, NwCreature damager)
    {
        int currentHpPercent = (int)((float)creature.HP / Math.Max(creature.MaxHP, 1) * 100);

        for (int i = 99; i > 0; i--)
        {
            // --- Spell triggers ---
            int spellId = creature.GetObjectVariable<LocalVariableInt>($"SpellID_{i}").Value;
            if (spellId != 0 && currentHpPercent <= i)
            {
                // Get target type for this trigger
                int targetType = creature.GetObjectVariable<LocalVariableInt>($"Target_{i}").Value;
                NwGameObject? spellTarget = ResolveSpellTarget(creature, damager, targetType);

                // Set OverrideAI to prevent normal AI from interrupting
                creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Value = 1;
                NWScript.DelayCommand(4.0f,
                    () => creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Delete());

                // Cheat-cast the spell
                creature.ClearActionQueue();
                if (spellTarget != null)
                {
                    NWScript.ActionCastSpellAtObject(spellId, spellTarget, (int)MetaMagic.Any, 1, 0, 0, 1);
                }

                // Delete the trigger (one-shot)
                creature.GetObjectVariable<LocalVariableInt>($"SpellID_{i}").Delete();
                creature.GetObjectVariable<LocalVariableInt>($"Target_{i}").Delete();
                return; // Only fire one trigger per damage event
            }

            // --- Ability/script triggers ---
            string abilityScript =
                creature.GetObjectVariable<LocalVariableString>($"AbilityID_{i}").Value ?? "";
            if (!string.IsNullOrEmpty(abilityScript) && currentHpPercent <= i)
            {
                int targetType = creature.GetObjectVariable<LocalVariableInt>($"Target_{i}").Value;
                NwGameObject? abilityTarget = ResolveSpellTarget(creature, damager, targetType);

                // Set OverrideAI
                creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Value = 1;
                NWScript.DelayCommand(4.0f,
                    () => creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Delete());

                // Store target on creature and execute script
                if (abilityTarget != null)
                {
                    creature.GetObjectVariable<LocalVariableObject<NwGameObject>>(abilityScript).Value =
                        abilityTarget;
                }

                NWScript.ExecuteScript(abilityScript, creature);

                // Delete the trigger (one-shot)
                creature.GetObjectVariable<LocalVariableString>($"AbilityID_{i}").Delete();
                creature.GetObjectVariable<LocalVariableInt>($"Target_{i}").Delete();
                return;
            }
        }
    }

    /// <summary>
    /// Resolves spell/ability target based on target type.
    /// Ports FindNPCSpellTarget() from ds_ai_include.nss (lines 1096-1114):
    /// 1 = Self, 2 = Current target, 3 = Random enemy, 4 = Ranged enemy, 5 = Nearest friendly
    /// </summary>
    private NwGameObject? ResolveSpellTarget(NwCreature creature, NwCreature damager, int targetType)
    {
        return targetType switch
        {
            1 => creature, // Self
            2 => _stateManager.GetState(creature)?.CurrentTarget ?? damager, // Current target
            3 => _targetingService.FindNearestEnemy(creature), // Random enemy
            4 => _targetingService.FindNearestEnemy(creature), // Ranged enemy (simplified)
            5 => creature.GetNearestCreatures(CreatureTypeFilter.Reputation(ReputationType.Friend))
                .FirstOrDefault(), // Nearest friendly
            _ => creature // Default: self
        };
    }
}
