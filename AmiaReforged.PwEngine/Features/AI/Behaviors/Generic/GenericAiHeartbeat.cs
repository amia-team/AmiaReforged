using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Behaviors;
using AmiaReforged.PwEngine.Features.AI.Core.Extensions;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI heartbeat handler - main AI execution loop.
/// Ports logic from ds_ai_heartbeat.nss and PerformAction() from ds_ai_include.nss.
/// </summary>
[ServiceBinding(typeof(IOnHeartbeatBehavior))]
public class GenericAiHeartbeat : IOnHeartbeatBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiTargetingService _targetingService;
    private readonly AiArchetypeService _archetypeService;
    private readonly AiSpellCacheService _spellCacheService;
    private readonly AiTalentService _talentService;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_heartbeat";

    public GenericAiHeartbeat(
        AiStateManager stateManager,
        AiTargetingService targetingService,
        AiArchetypeService archetypeService,
        AiSpellCacheService spellCacheService,
        AiTalentService talentService)
    {
        _stateManager = stateManager;
        _targetingService = targetingService;
        _archetypeService = archetypeService;
        _spellCacheService = spellCacheService;
        _talentService = talentService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnHeartbeat(CreatureEvents.OnHeartbeat eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        AiState? state = _stateManager.GetState(creature);
        if (state == null) return;

        // Check for sleep mode (>5 inactive heartbeats)
        if (state.IsSleeping)
        {
            state.IncrementInactivity();

            // Warn DMs at 100 heartbeats (10 minutes of inactivity)
            if (state.InactiveHeartbeats == 100)
            {
                string message = $"DS AI message: {creature.Name} in {creature.Area?.Name ?? "unknown area"} has been inactive for 10 minutes now.";
                foreach (NwPlayer dm in NwModule.Instance.Players.Where(p => p.IsDM))
                {
                    dm.SendServerMessage(message);
                }
            }

            return;
        }

        // Execute AI action
        bool didSomething = PerformAction(creature, state);

        // Update activity state
        if (didSomething)
        {
            state.MarkActive();
        }
        else
        {
            state.IncrementInactivity();
        }
    }

    /// <summary>
    /// Main AI action logic - ports PerformAction() from ds_ai_include.nss (lines 550-640).
    /// Returns true if an action was performed.
    /// </summary>
    private bool PerformAction(NwCreature creature, Core.Models.AiState state)
    {
        // Get or acquire target
        NwGameObject? target = _targetingService.GetValidTarget(creature, state.CurrentTarget);

        if (target == null)
        {
            // No valid target found
            state.CurrentTarget = null;
            return false;
        }

        // Update current target
        state.CurrentTarget = target;

        // Get archetype to determine behavior
        IAiArchetype? archetype = _archetypeService.GetArchetype(creature);
        int archetypeValue = archetype != null ?
            _archetypeService.GetArchetype(creature) != null ? 5 : 5 : 5; // Default to hybrid

        // Try special attack (d12 roll)
        if (_talentService.TrySpecialAttack(creature, target))
        {
            return true;
        }

        // Get spell cache
        CreatureSpellCache spellCache = _spellCacheService.GetOrCreateCache(creature);

        // Casters (archetype 7-10) prioritize spells
        if (archetypeValue >= 7 && spellCache.MaxCasterLevel > 0)
        {
            if (TryDoSpellCast(creature, target, state, spellCache))
            {
                return true;
            }
        }

        // Melee attack as fallback
        if (DoAttack(creature, target))
        {
            return true;
        }

        // Hybrids and lower casters can try spells if melee fails
        if (archetypeValue < 7 && spellCache.MaxCasterLevel > 0)
        {
            if (TryDoSpellCast(creature, target, state, spellCache))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Performs a melee attack action.
    /// Ports DoAttack() from ds_ai_include.nss (lines 707-724).
    /// </summary>
    private bool DoAttack(NwCreature creature, NwGameObject target)
    {
        if (target == null) return false;

        // Check if already attacking this target
        if (creature.AttackTarget == target)
        {
            return true;
        }

        // Clear actions and attack
        creature.ClearActionQueue();
        creature.ActionAttackTarget(target);

        return true;
    }

    /// <summary>
    /// Attempts to cast a spell at the target.
    /// Ports DoSpellCast() logic from ds_ai_include.nss (lines 762-855).
    /// </summary>
    private bool TryDoSpellCast(NwCreature creature, NwGameObject target,
        Core.Models.AiState state, Core.Models.CreatureSpellCache spellCache)
    {
        if (target is not NwCreature targetCreature) return false;

        // Start with highest caster level and work down
        for (int cl = spellCache.MaxCasterLevel; cl > 0; cl--)
        {
            if (!spellCache.SpellsByCasterLevel.TryGetValue(cl, out List<Spell>? spells))
                continue;

            foreach (Spell spell in spells)
            {
                // Check spam limit
                if (spellCache.HasReachedSpamLimit(spell))
                    continue;

                // Check if we have spell uses
                NwSpell? nwSpell = NwSpell.FromSpellId((int)spell);
                if (nwSpell == null || !creature.HasSpellUse(nwSpell))
                    continue;

                // Skip if same as last spell (prevent immediate re-cast of buffs)
                if (state.LastSpellCast == spell)
                    continue;

                // Check if valid for target (undead filtering)
                if (!spell.IsValidForTarget(targetCreature))
                    continue;

                // Determine if this is an attack spell
                bool isAttackSpell = spell.IsAttackSpell();

                // Cast the spell
                creature.ClearActionQueue();

                if (isAttackSpell)
                {
                    creature.ActionCastSpellAt(spell, targetCreature);
                }
                else
                {
                    // Buff/heal - cast on self or allies
                    if (spell.IsHealingSpell() && creature.HP < creature.MaxHP * 0.5)
                    {
                        creature.ActionCastSpellAt(spell, creature);
                    }
                    else if (spell.IsBuffSpell())
                    {
                        creature.ActionCastSpellAt(spell, creature);
                    }
                    else
                    {
                        continue; // Skip non-applicable spells
                    }
                }

                // Track spell usage
                state.LastSpellCast = spell;
                if (!spellCache.SpellUsageCount.ContainsKey(spell))
                {
                    spellCache.SpellUsageCount[spell] = 0;
                }
                spellCache.SpellUsageCount[spell]++;

                return true;
            }
        }

        return false;
    }
}
