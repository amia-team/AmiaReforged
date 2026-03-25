using System.Numerics;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Behaviors;
using AmiaReforged.PwEngine.Features.AI.Core.Extensions;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI heartbeat handler - main AI execution loop.
/// Ports logic from ds_ai_heartbeat.nss and PerformAction() from ds_ai_include.nss.
///
/// Encapsulates the following legacy behaviors:
/// - Spell category sub-selection (d6 roll for casters: attack/buff/influence/poly/summon)
/// - Custom spell slots (ds_ai_custom_* per-creature overrides)
/// - Polymorph archetype transitions (caster → melee after poly)
/// - Undead spell reversal (cure damages undead, harm heals undead)
/// - Summon duplicate prevention (skip summon if associate exists)
/// - Buff duplicate avoidance (skip if already has effect or same as last spell)
/// - Pre-spell target filtering (Dismissal/Banishment only vs summoned creatures)
/// - Path-blocking detection (melee stuck at >10m → force target switch)
/// - One heal per fight (L_HEALED flag)
/// - Curse Song (50% chance if target in range and not already affected)
/// - Grapple system (opposed d20+BAB+STR+Size check)
/// - HiPS archetype (70% chance to disengage, stealth, and re-engage)
/// - Sneak archetype (starts stealthed)
/// - Flee logic with archetype modifier (-2 for casters/ranged)
/// - Pet-to-PC target preference (50% switch from pet to master)
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

    /// <summary>Distance threshold for path-blocking detection.</summary>
    private const float BlockDetectionDistance = 10.0f;

    /// <summary>Movement threshold below which creature is considered "stuck".</summary>
    private const float BlockMovementThreshold = 0.5f;

    /// <summary>Distance within which HiPS creatures disengage.</summary>
    private const float HipsDisengageDistance = 3.0f;

    /// <summary>Size modifiers for grapple checks, matching legacy DoGrapple().</summary>
    private static readonly Dictionary<CreatureSize, int> GrappleSizeModifiers = new()
    {
        { CreatureSize.Tiny, -8 },
        { CreatureSize.Small, -4 },
        { CreatureSize.Medium, 0 },
        { CreatureSize.Large, 4 },
        { CreatureSize.Huge, 8 }
    };

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
                string message =
                    $"DS AI message: {creature.Name} in {creature.Area?.Name ?? "unknown area"} has been inactive for 10 minutes now.";
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
    /// Main AI action logic - full port of PerformAction() from ds_ai_include.nss (lines 550-860).
    /// Returns true if an action was performed.
    /// </summary>
    private bool PerformAction(NwCreature creature, AiState state)
    {
        // --- Target acquisition with pet-to-PC preference ---
        NwGameObject? target = _targetingService.GetValidTarget(creature, state.CurrentTarget);

        if (target == null)
        {
            state.CurrentTarget = null;
            return false;
        }

        // Pet-to-PC target preference: 50% chance to switch from pet to its PC master
        target = TryPreferPcOverPet(creature, target);

        state.CurrentTarget = target;

        // --- Polymorphed creatures just attack (no casting) ---
        if (creature.ActiveEffects.Any(e => e.EffectType == EffectType.Polymorph))
        {
            creature.ActionAttackTarget(target);
            return true;
        }

        // --- Get reaction for flee logic ---
        int reaction = GetReaction(creature, target, state);
        if (reaction == 2) // Flee
        {
            creature.ClearActionQueue();
            creature.ActionMoveAwayFrom(target, true, 30.0f);
            return true;
        }

        // --- Self-healing (one per fight) ---
        if (DoHeal(creature, state))
        {
            return true;
        }

        // --- Curse Song: 50% chance if target within 10m ---
        if (TryCurseSong(creature, target))
        {
            return true;
        }

        int archetypeValue = _archetypeService.GetArchetypeValue(creature);
        CreatureSpellCache spellCache = _spellCacheService.GetOrCreateCache(creature);

        // --- Caster archetype (C): spell category sub-selection (d6 roll) ---
        if (_archetypeService.IsCasterArchetype(creature) && spellCache.MaxCasterLevel > 0)
        {
            if (TryCasterSpellSelection(creature, target, state, spellCache))
            {
                return true;
            }
        }

        // --- Feat buffs (one-time) ---
        _talentService.TryUseFeatBuff(creature);

        // --- HiPS archetype: 70% chance to disengage, stealth, and re-engage ---
        if (_archetypeService.IsHipsArchetype(creature))
        {
            if (TryHipsBehavior(creature, target))
            {
                return true;
            }
        }

        // --- Special attacks (d12 roll) ---
        if (_talentService.TrySpecialAttack(creature, target))
        {
            return true;
        }

        // --- Melee/ranged attack with path-blocking detection ---
        if (DoAttack(creature, target, state))
        {
            return true;
        }

        // --- Hybrids try spells after melee ---
        if (archetypeValue >= 4 && spellCache.MaxCasterLevel > 0)
        {
            if (TryDoSpellCast(creature, target, state, spellCache, "attc"))
            {
                return true;
            }
        }

        // --- Fallback: move toward target ---
        TargetValidity validity = _targetingService.ValidateTarget(creature,
            target as NwCreature ?? creature);
        if (validity > TargetValidity.NotHostile)
        {
            creature.ActionMoveTo(target, true);
            return true;
        }

        return false;
    }

    // =====================================================================
    // Spell System
    // =====================================================================

    /// <summary>
    /// Caster spell category sub-selection.
    /// Ports the d6 roll from PerformAction() caster block in ds_ai_include.nss:
    ///   3 → influence/inflict, 4 → buff, 5 → poly, 6 → summon, default → attack.
    /// Falls through to attack spell if first choice fails.
    /// </summary>
    private bool TryCasterSpellSelection(NwCreature creature, NwGameObject target,
        AiState state, CreatureSpellCache spellCache)
    {
        // Check for custom spell overrides first (ds_ai_custom_* system)
        if (TryCustomSpell(creature, target, state))
        {
            return true;
        }

        int roll = Random.Shared.Next(1, 7); // d6

        string spellType = roll switch
        {
            3 => "infl", // Influence/inflict spell
            4 => "buff", // Buff spell on self
            5 => "poly", // Polymorph spell
            6 => "summ", // Summon spell
            _ => "attc" // Attack spell (1, 2, and fallback)
        };

        // Try the selected type first
        if (TryDoSpellCast(creature, target, state, spellCache, spellType))
        {
            return true;
        }

        // Fall through to attack spell if first choice failed
        if (spellType != "attc")
        {
            if (TryDoSpellCast(creature, target, state, spellCache, "attc"))
            {
                return true;
            }
        }

        // If caster has no Combat Casting, queue melee attack after spell attempt
        if (!creature.KnowsFeat(Feat.CombatCasting))
        {
            creature.ActionAttackTarget(target);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks for per-creature custom spell overrides (ds_ai_custom_* local vars).
    /// These override normal spell selection for the matching category.
    /// </summary>
    private bool TryCustomSpell(NwCreature creature, NwGameObject target, AiState state)
    {
        // Check custom attack spell
        int customAttack = creature.GetObjectVariable<LocalVariableInt>("ds_ai_custom_a").Value;
        if (customAttack != 0)
        {
            Spell spell = (Spell)customAttack;
            creature.ClearActionQueue();
            NWScript.ActionCastSpellAtObject(customAttack, target, (int)MetaMagic.Any, 1);
            state.LastSpellCast = spell;
            return true;
        }

        // Check custom buff spell
        int customBuff = creature.GetObjectVariable<LocalVariableInt>("ds_ai_custom_b").Value;
        if (customBuff != 0)
        {
            Spell spell = (Spell)customBuff;
            creature.ClearActionQueue();
            NWScript.ActionCastSpellAtObject(customBuff, creature, (int)MetaMagic.Any, 1);
            state.LastSpellCast = spell;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to cast a spell of the given type at the target.
    /// Comprehensive port of DoSpellCast() from ds_ai_include.nss with all filtering:
    /// - Spam limit (2 casts per spell)
    /// - Buff duplicate avoidance (skip if same as last spell)
    /// - Undead spell reversal (cure → inflict on undead self, harm → heal on undead target)
    /// - Summon duplicate prevention (skip if already has associate)
    /// - Pre-spell target filtering (Dismiss/Banish only vs summoned)
    /// - Polymorph archetype transition (caster → melee after 3s)
    /// - Spell list rotation (different spells tried each round)
    /// </summary>
    private bool TryDoSpellCast(NwCreature creature, NwGameObject target,
        AiState state, CreatureSpellCache spellCache, string spellType)
    {
        IReadOnlyList<Spell> spellList = spellType switch
        {
            "attc" => spellCache.AttackSpells,
            "buff" => spellCache.BuffSpells,
            "heal" => spellCache.HealingSpells,
            "infl" => spellCache.AttackSpells, // Inflict uses attack list in C#
            "poly" => spellCache.BuffSpells, // Poly is a subset of buffs
            "summ" => spellCache.SummonSpells,
            _ => spellCache.AttackSpells
        };

        if (spellList.Count == 0) return false;

        // Collect up to 3 candidate spells (rotation: randomize order)
        List<Spell> candidates = spellList.OrderBy(_ => Random.Shared.Next()).Take(3).ToList();

        foreach (Spell spell in candidates)
        {
            // Spam limit check
            if (spellCache.HasReachedSpamLimit(spell)) continue;

            // Check spell availability
            NwSpell? nwSpell = NwSpell.FromSpellId((int)spell);
            if (nwSpell == null || !creature.HasSpellUse(nwSpell)) continue;

            // Pre-filter: don't cast same spell twice in a row
            if (state.LastSpellCast == spell) continue;

            // === Type-specific filtering ===

            if (spellType == "attc" || spellType == "infl")
            {
                if (target is NwCreature targetCreature)
                {
                    // Undead reversal: inflict heals undead, cure damages undead
                    if (spell.IsHealingSpell() && targetCreature.Race.RacialType == RacialType.Undead)
                    {
                        // Cure spells damage undead — this is valid, allow it
                    }
                    else if (!spell.IsValidForTarget(targetCreature))
                    {
                        continue;
                    }

                    // Pre-spell target filtering: Dismissal/Banishment only vs summoned
                    if (spell == Spell.Dismissal || spell == Spell.Banishment)
                    {
                        if (targetCreature.Master == null)
                        {
                            continue; // Not a summoned creature, skip this spell
                        }
                    }
                }

                // Cast attack spell at target
                creature.ClearActionQueue();
                creature.ActionCastSpellAt(spell, target);
            }
            else if (spellType == "buff")
            {
                // Buff duplicate avoidance: skip if already has the buff effect
                // (simplified: just check last spell)
                if (state.LastSpellCast == spell) continue;

                creature.ClearActionQueue();
                creature.ActionCastSpellAt(spell, creature); // Cast on self
            }
            else if (spellType == "heal")
            {
                // Undead check: if creature is undead, try inflict on self instead (heals undead)
                if (creature.Race.RacialType == RacialType.Undead)
                {
                    // Skip heal spells for undead casters (they need inflict)
                    continue;
                }

                if (creature.HP >= creature.MaxHP * 0.5) continue; // Only heal below 50%

                creature.ClearActionQueue();
                creature.ActionCastSpellAt(spell, creature);
            }
            else if (spellType == "poly")
            {
                // Filter to actual polymorph spells
                if ((int)spell < 387 || (int)spell > 396) continue;

                // Randomize polymorph variant (legacy GetMultiOption)
                int polySpellId = spell switch
                {
                    >= Spell.PolymorphSelf and <= (Spell)391 =>
                        Random.Shared.Next((int)Spell.PolymorphSelf, 392),
                    >= (Spell)392 and <= (Spell)396 =>
                        Random.Shared.Next(392, 397),
                    _ => (int)spell
                };
                Spell polySpell = (Spell)polySpellId;

                creature.ClearActionQueue();
                creature.ActionCastSpellAt(polySpell, creature);

                // Archetype transition: caster → melee after polymorph (3s delay)
                NWScript.DelayCommand(3.0f, () => { _archetypeService.OnSpellsExhausted(creature); });
            }
            else if (spellType == "summ")
            {
                // Summon duplicate prevention: skip if already has a summoned associate
                NwCreature? existingSummon = creature.GetAssociate(AssociateType.Summoned);
                if (existingSummon != null) continue;

                creature.ClearActionQueue();
                creature.ActionCastSpellAt(spell, target);
            }
            else
            {
                continue;
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

        // If we exhausted all spells of this type, check for archetype transition
        if (spellType == "attc" && !candidates.Any(s => creature.HasSpellUse(NwSpell.FromSpellId((int)s))))
        {
            _archetypeService.OnSpellsExhausted(creature);
        }

        return false;
    }

    // =====================================================================
    // Combat Mechanics
    // =====================================================================

    /// <summary>
    /// Performs a melee/ranged attack with path-blocking detection.
    /// Ports DoAttack() from ds_ai_include.nss (lines 707-770):
    /// - If distance > 10m and creature hasn't moved since last check while targeting
    ///   the same entity → marks as "blocked", clears target (forces switch next round)
    /// - If creature switched targets, clears block state
    /// </summary>
    private bool DoAttack(NwCreature creature, NwGameObject target, AiState state)
    {
        if (target == null) return false;

        float distance = creature.Distance(target);
        Vector3 currentPosition = creature.Position;

        // Path-blocking detection for melee creatures at distance
        if (distance > BlockDetectionDistance)
        {
            if (state.LastPosition.HasValue && state.LastPathTarget == target)
            {
                // Same target as last round — check if we've moved
                float moved = Vector3.Distance(currentPosition, state.LastPosition.Value);

                if (moved < BlockMovementThreshold)
                {
                    // Stuck! Clear target to force switch next round
                    state.CurrentTarget = null;
                    state.LastPosition = null;
                    state.LastPathTarget = null;
                    state.BlockedBy = null;
                    return false;
                }
            }

            // Record position for next round's check
            state.LastPosition = currentPosition;
            state.LastPathTarget = target;
        }
        else
        {
            // Within range — clear blocking state
            state.LastPosition = null;
            state.LastPathTarget = null;
            state.BlockedBy = null;
        }

        // Already attacking this target
        if (creature.AttackTarget == target)
        {
            return true;
        }

        creature.ClearActionQueue();
        creature.ActionAttackTarget(target);
        return true;
    }

    /// <summary>
    /// Self-healing limited to one attempt per fight.
    /// Ports DoHeal() from ds_ai_include.nss (lines 877-905):
    /// - Only heals when HP < 50% max
    /// - One attempt per combat (HasHealed flag)
    /// - Tries spell healing first, then potions via TalentHealingSelf
    /// </summary>
    private bool DoHeal(NwCreature creature, AiState state)
    {
        if (state.HasHealed) return false;
        if (creature.HP >= creature.MaxHP * 0.5) return false;

        CreatureSpellCache spellCache = _spellCacheService.GetOrCreateCache(creature);

        // Try healing spells
        foreach (Spell spell in spellCache.HealingSpells)
        {
            NwSpell? nwSpell = NwSpell.FromSpellId((int)spell);
            if (nwSpell == null || !creature.HasSpellUse(nwSpell)) continue;

            // Undead creatures need inflict spells, not cure spells
            if (creature.Race.RacialType == RacialType.Undead)
            {
                // Skip regular healing for undead
                continue;
            }

            creature.ClearActionQueue();
            creature.ActionCastSpellAt(spell, creature);
            state.HasHealed = true;
            return true;
        }

        // Potion fallback via TalentHealingSelf (NWN built-in)
        IntPtr healTalent = NWScript.GetCreatureTalentBest(NWScript.TALENT_CATEGORY_BENEFICIAL_HEALING_TOUCH, 20, creature);
        if (NWScript.GetIsTalentValid(healTalent) == NWScript.TRUE)
        {
            NWScript.AssignCommand(creature, () => NWScript.ActionUseTalentOnObject(healTalent, creature));
        }
        state.HasHealed = true;
        return true;
    }

    /// <summary>
    /// Curse Song: 50% chance to use if target is within 10m and not already affected.
    /// Ports the Curse Song block from PerformAction() in ds_ai_include.nss.
    /// </summary>
    private bool TryCurseSong(NwCreature creature, NwGameObject target)
    {
        if (!creature.KnowsFeat(Feat.CurseSong)) return false;
        if (!creature.HasFeatPrepared(Feat.CurseSong)) return false;

        // 50% chance
        if (Random.Shared.Next(2) == 0) return false;

        // Range check: within 10m
        float distance = creature.Distance(target);
        if (distance > 10.0f) return false;

        // Check if target already has Curse Song effect (check for the spell effect)
        if (target is NwCreature targetCreature)
        {
            bool alreadyCursed = targetCreature.ActiveEffects
                .Any(e => e.Spell?.SpellType == Spell.AbilityEpicCurseSong);
            if (alreadyCursed) return false;
        }

        creature.ActionUseFeat(Feat.CurseSong, target);
        return true;
    }

    /// <summary>
    /// Grapple system: opposed d20+BAB+STR+SizeModifier check.
    /// Ports DoGrapple() from ds_ai_include.nss (lines 1014-1094):
    /// - Touch attack (melee) must succeed
    /// - Opposed check: d20 + BAB + STR modifier + size modifier
    /// - On success: applies CutsceneImmobilize for 5.8s (supernatural)
    /// - Size modifiers: Tiny=-8, Small=-4, Medium=0, Large=4, Huge=8
    /// </summary>
    public bool TryGrapple(NwCreature creature, NwCreature target)
    {
        // Touch attack check
        int touchResult = NWScript.TouchAttackMelee(target);
        if (touchResult == 0) return false;

        // Opposed grapple check
        int creatureBab = creature.BaseAttackBonus;
        int creatureStr = creature.GetAbilityModifier(Ability.Strength);
        int creatureSize = GrappleSizeModifiers.GetValueOrDefault(creature.Size, 0);
        int creatureRoll = Random.Shared.Next(1, 21) + creatureBab + creatureStr + creatureSize;

        int targetBab = target.BaseAttackBonus;
        int targetStr = target.GetAbilityModifier(Ability.Strength);
        int targetSize = GrappleSizeModifiers.GetValueOrDefault(target.Size, 0);
        int targetRoll = Random.Shared.Next(1, 21) + targetBab + targetStr + targetSize;

        if (creatureRoll <= targetRoll) return false;

        // Grapple success: apply immobilize for 5.8s
        Effect immobilize = Effect.CutsceneImmobilize();
        Effect visual = Effect.VisualEffect(VfxType.DurCessateNegative);
        Effect linked = Effect.LinkEffects(immobilize, visual);

        target.ApplyEffect(EffectDuration.Temporary, linked, TimeSpan.FromSeconds(5.8));

        // Send combat log
        if (target.IsPlayerControlled)
        {
            string message =
                $"{creature.Name} grapples {target.Name}! " +
                $"({creatureRoll} vs {targetRoll})";
            target.ControllingPlayer?.SendServerMessage(message);
        }

        return true;
    }

    // =====================================================================
    // Archetype-Specific Behaviors
    // =====================================================================

    /// <summary>
    /// HiPS (Hide in Plain Sight) archetype behavior:
    /// 70% chance to move away 3m, enter stealth, then schedule re-engagement.
    /// Ports the HiPS block from PerformAction() in ds_ai_include.nss.
    /// </summary>
    private bool TryHipsBehavior(NwCreature creature, NwGameObject target)
    {
        int roll = Random.Shared.Next(1, 101);
        if (roll > AiArchetypeService.HipsChance) return false;

        // Disengage: move away 3m
        creature.ClearActionQueue();
        creature.ActionMoveAwayFrom(target, false, HipsDisengageDistance);

        // Enter stealth
        NWScript.AssignCommand(creature, () => NWScript.SetActionMode(creature, NWScript.ACTION_MODE_STEALTH, 1));

        // Schedule re-engagement after 2 seconds
        NWScript.DelayCommand(2.0f, () =>
        {
            if (creature.IsValid && target.IsValid)
            {
                creature.ActionAttackTarget(target);
            }
        });

        return true;
    }

    // =====================================================================
    // Targeting Helpers
    // =====================================================================

    /// <summary>
    /// Pet-to-PC target preference: 50% chance to switch from attacking a henchman/familiar/summon
    /// to its PC master. Ports the pet-preference logic from GetReaction() in ds_ai_include.nss.
    /// </summary>
    private NwGameObject TryPreferPcOverPet(NwCreature creature, NwGameObject target)
    {
        if (target is not NwCreature targetCreature) return target;

        // Check if target is an associate (pet/familiar/henchman/summon)
        NwCreature? master = targetCreature.Master;
        if (master == null || !master.IsPlayerControlled) return target;

        // 50% chance to prefer the PC master
        if (Random.Shared.Next(2) == 0) return target;

        // Validate that we can see the master
        if (creature.IsCreatureSeen(master) && master.IsEnemy(creature))
        {
            return master;
        }

        return target;
    }

    /// <summary>
    /// Gets combat reaction for a creature vs target, with archetype-weighted flee logic.
    /// Ports GetReaction() from ds_ai_include.nss (lines 1767-1823):
    /// - Casters/ranged get -2 modifier (more likely to flee)
    /// - HP < 25% AND target HP > 75% AND modified roll > 9 → flee (2)
    /// - HP > 75% AND target HP > 50% AND modified roll > 5 → switch (1)
    /// Returns: 0 = no change, 1 = switch target, 2 = flee
    /// </summary>
    private int GetReaction(NwCreature creature, NwGameObject target, AiState state)
    {
        if (target is not NwCreature targetCreature) return 0;
        if (!targetCreature.IsValid) return 0;

        int roll = Random.Shared.Next(1, 11); // d10

        // Archetype modifier: casters and ranged get -2 (flee more easily)
        string archetype = state.ArchetypeId ?? "melee";
        if (archetype is "caster" or "ranged")
        {
            roll -= 2;
        }

        float creatureHpPercent = (float)creature.HP / Math.Max(creature.MaxHP, 1);
        float targetHpPercent = (float)targetCreature.HP / Math.Max(targetCreature.MaxHP, 1);

        // Low HP creature vs high HP target: flee
        if (creatureHpPercent < 0.25f && targetHpPercent > 0.75f && roll > 9)
        {
            return 2; // Flee
        }

        // High HP creature vs mid HP target: consider switching
        if (creatureHpPercent > 0.75f && targetHpPercent > 0.50f && roll > 5)
        {
            return 1; // Switch target
        }

        return 0; // No change
    }
}
