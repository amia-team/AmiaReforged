# AI System Remaster - Architecture & Implementation Plan

**Project**: AmiaReforged.PwEngine
**Target**: Refactor legacy NWN AI scripts to modular C# architecture
**Created**: November 22, 2025
**Status**: Planning & Design Phase

---

## Executive Summary

The current AI system uses legacy `.nss` scripts with monolithic includes (`ds_ai_include.nss` - 1,278 lines) and complex boss-specific scripts (e.g., `ds_ai2_necroboss.nss` - 800 lines). This refactoring transforms the system into lightweight, composable, object-oriented C# that leverages Anvil's event system and dependency injection while maintaining server-scale performance.

### Goals

✅ **Composability** - Build AI from reusable behavior components instead of monolithic scripts
✅ **Performance** - Service-level caching and intelligent activity tracking to minimize overhead
✅ **Maintainability** - Clear separation of concerns with typed interfaces and DI
✅ **Safety** - Feature-flagged rollout (dev/test only, disabled on live)
✅ **Gradual Migration** - Coexist with legacy scripts during transition period

---

## Current System Analysis

### Legacy Architecture Problems

1. **Monolithic Include Library** (`ds_ai_include.nss`)
   - 1,278 lines of tightly coupled procedural code
   - No clear separation between targeting, spell casting, melee combat
   - Hard to test, modify, or extend individual behaviors
   - Global state management via string-based local variables

2. **Boss Script Duplication**
   - Each boss (necroboss, dampworm, whitedragon) has 400-800 line scripts
   - Shared patterns (HP thresholds, phase management) copied between scripts
   - Maintenance nightmare when fixing common bugs

3. **Performance Concerns**
   - Spell list parsing happens repeatedly via `ParseSpellList()`
   - No caching strategy - rebuilds spell catalogs frequently
   - Heartbeat runs on all creatures even when inactive
   - No distinction between active combat and idle states

4. **Type Safety Issues**
   - Heavy use of `GetLocalInt/String/Object` with magic strings
   - Constants like `L_CURRENTTARGET`, `L_ARCHETYPE` error-prone
   - No compile-time validation of AI state

### Legacy System Inventory

**Core AI Scripts** (Generic behaviors):
- `ds_ai_spawn.nss` - OnSpawn buff/initialization
- `ds_ai_heartbeat.nss` - Main AI loop with inactivity tracking
- `ds_ai_perceive.nss` - OnPerception target acquisition
- `ds_ai_damaged.nss` - OnDamaged reactions
- `ds_ai_attacked.nss` - OnAttacked target switching
- `ds_ai_endround.nss` - OnCombatRoundEnd checks
- `ds_ai_blocked.nss` - OnBlocked pathfinding
- `ds_ai_convo.nss` - OnConversation interrupts
- `ds_ai_spellcast.nss` - OnSpellCastAt reactions
- `ds_ai_death.nss` - OnDeath cleanup
- `ds_ai_cleanup.nss` - Manual cleanup handler

**Specialized AI Scripts** (Boss/encounter-specific):
- `ds_ai2_necroboss.nss` - Necromancer lich boss (10 phase thresholds)
- `ds_ai2_dampworm.nss` - Purple worm (damp cave variant)
- `ds_ai2_heartworm.nss` - Purple worm heartbeat
- `ds_ai2_deathworm.nss` - Purple worm death
- `ds_ai2_damwhited.nss` - White dragon (damp cave variant)
- `ds_ai2_deathinva.nss` - Invasion death handler
- `ds_ai2_heartlich.nss` - Lich heartbeat
- `ds_ai2_gibboss.nss` - Gibberling boss mechanics
- `ds_ai2_spawn.nss` - Special spawn behaviors
- `ds_ai2_special.nss` - Special attack handlers
- `ds_ai2_spells.nss` - Custom spell effects
- `ds_ai2_poly.nss` - Polymorph handling
- `ds_ai2_raise.nss` - Resurrection mechanics

**Core Include Library**:
- `ds_ai_include.nss` - 1,278 lines of shared functions

---

## Target Architecture

### Design Principles

1. **Composition Over Inheritance**
   - AI behaviors built from small, focused components
   - Chain components together to create complex behaviors
   - Avoid deep inheritance hierarchies

2. **Strategy Pattern for Behaviors**
   - Each behavior aspect (targeting, combat, movement) is a swappable strategy
   - Archetypes select appropriate strategies at initialization
   - Easy to test behaviors in isolation

3. **Service-Level Caching**
   - Centralized caches in DI services (not per-creature local variables)
   - `Dictionary<uint, T>` keyed by creature ID
   - Lifecycle managed by services, cleaned up on death

4. **Event-Driven Execution**
   - Leverage Anvil's native event system (OnHeartbeat, OnDamaged, etc.)
   - No polling loops or manual script calls
   - Behaviors subscribe to relevant events only

5. **Feature Flagging**
   - All new AI code checks `SERVER_MODE` environment variable
   - Disabled on `"live"` servers automatically
   - Allows parallel development and testing without production risk

### Directory Structure

```
AmiaReforged.PwEngine/Features/AI/
├── AI_REMASTER.md                          # This document
├── AiMasterService.cs                      # Event orchestrator (existing)
├── AiBlindnessService.cs                   # Special condition handler (existing)
│
├── Core/                                   # NEW: Core services & interfaces
│   ├── Services/
│   │   ├── AiSpellCacheService.cs         # Centralized spell list cache
│   │   ├── AiTargetingService.cs          # Shared target validation/selection
│   │   ├── AiArchetypeService.cs          # Archetype registration & lookup
│   │   ├── AiActivityTrackerService.cs    # Active/dormant creature tracking
│   │   └── AiTalentService.cs             # Feat/ability usage helpers
│   │
│   ├── Interfaces/
│   │   ├── IAiArchetype.cs                # Archetype contract
│   │   ├── IAiBehaviorComponent.cs        # Behavior component base
│   │   ├── ITargetingStrategy.cs          # Target selection strategy
│   │   ├── ICombatTactic.cs               # Combat behavior tactic
│   │   └── IMovementStrategy.cs           # Movement/positioning strategy
│   │
│   ├── Models/
│   │   ├── CreatureSpellCache.cs          # Spell list data structure
│   │   ├── AiTarget.cs                    # Target wrapper with metadata
│   │   ├── AiArchetypeConfig.cs           # Archetype configuration
│   │   └── AiActivityState.cs             # Creature activity tracking
│   │
│   └── Extensions/
│       ├── NwCreatureAiExtensions.cs      # Extension methods for AI state
│       └── SpellExtensions.cs             # Spell filtering/categorization
│
├── Archetypes/                            # NEW: Archetype implementations
│   ├── MeleeArchetype.cs                  # Pure melee fighter (archetype 1-3)
│   ├── HybridArchetype.cs                 # Mixed fighter/caster (archetype 4-6)
│   ├── CasterArchetype.cs                 # Pure spellcaster (archetype 7-10)
│   ├── SupportArchetype.cs                # Healer/buffer
│   └── SummonerArchetype.cs               # Summoning specialist
│
├── Components/                            # NEW: Reusable behavior components
│   ├── Targeting/
│   │   ├── NearestEnemyTargeting.cs       # Simple nearest-enemy selection
│   │   ├── ThreatBasedTargeting.cs        # Aggro-based target switching
│   │   └── PlayerPreferenceTargeting.cs   # Prefer PC targets over NPCs
│   │
│   ├── Combat/
│   │   ├── MeleeAttackTactic.cs           # Basic melee attack execution
│   │   ├── SpellCastingTactic.cs          # Spell selection & casting
│   │   ├── HealingTactic.cs               # Self/ally healing
│   │   ├── BuffingTactic.cs               # Self-buff application
│   │   ├── SummoningTactic.cs             # Summon creature management
│   │   └── SpecialAttackTactic.cs         # Feat-based attacks (knockdown, etc.)
│   │
│   ├── Movement/
│   │   ├── AggressiveMovement.cs          # Chase targets, close distance
│   │   ├── DefensiveMovement.cs           # Kite, maintain distance
│   │   └── TacticalMovement.cs            # Cover, flanking positioning
│   │
│   └── Conditions/
│       ├── LowHealthCondition.cs          # Trigger at HP thresholds
│       ├── NoTargetCondition.cs           # Trigger when no valid targets
│       └── AllyCountCondition.cs          # Trigger based on ally presence
│
├── Bosses/                                # NEW: Boss-specific AI implementations
│   ├── PhaseBasedAiBehavior.cs            # Base class for phase bosses
│   ├── NecroLichBossAi.cs                 # Necromancer lich (replaces ds_ai2_necroboss)
│   ├── PurpleWormAi.cs                    # Purple worm encounters
│   ├── WhiteDragonAi.cs                   # White dragon boss
│   └── GibberlingSwarmAi.cs               # Gibberling boss mechanics
│
├── Behaviors/                             # EXISTING: Event handler interfaces
│   ├── Generic/
│   │   └── GenericOnCombatRoundEnd.cs     # Existing generic handler
│   ├── IOnBlockedBehavior.cs
│   ├── IOnCombatRoundEndBehavior.cs
│   ├── IOnConversationBehavior.cs
│   ├── IOnDamagedBehavior.cs
│   ├── IOnDeathBehavior.cs
│   ├── IOnDisturbedBehavior.cs
│   ├── IOnHeartbeatBehavior.cs
│   ├── IOnPerceptionBehavior.cs
│   ├── IOnPhysicalAttackedBehavior.cs
│   ├── IOnRestedBehavior.cs
│   ├── IOnSpawnBehavior.cs
│   ├── IOnSpellCastAtBehavior.cs
│   └── IOnUserDefined.cs
│
└── LegacyScripts/                         # EXISTING: Legacy .nss files (preserved)
    ├── ds_ai_include.nss
    ├── ds_ai_*.nss                        # Generic AI scripts
    └── ds_ai2_*.nss                       # Specialized AI scripts
```

---

## Implementation Phases

### Phase 1: Core Infrastructure (Week 1)

**Goal**: Establish service layer and basic interfaces

#### 1.1 Core Services

**AiSpellCacheService.cs**
```csharp
[ServiceBinding(typeof(AiSpellCacheService))]
public class AiSpellCacheService
{
    private readonly Dictionary<uint, CreatureSpellCache> _spellCaches = new();
    private readonly bool _isEnabled;

    public AiSpellCacheService()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public CreatureSpellCache GetOrCreateCache(NwCreature creature)
    {
        if (!_isEnabled) return CreatureSpellCache.Empty;

        if (!_spellCaches.TryGetValue(creature.UUID, out var cache))
        {
            cache = BuildSpellCache(creature);
            _spellCaches[creature.UUID] = cache;
        }
        return cache;
    }

    public void InvalidateCache(NwCreature creature)
    {
        _spellCaches.Remove(creature.UUID);
    }

    private CreatureSpellCache BuildSpellCache(NwCreature creature)
    {
        // Port logic from MakeSpellList() in ds_ai_include.nss
        // Scan creature's spell list, categorize by level and type
    }
}
```

**AiTargetingService.cs**
```csharp
[ServiceBinding(typeof(AiTargetingService))]
public class AiTargetingService
{
    private readonly bool _isEnabled;

    public AiTargetingService()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    // Port GetTarget(), GetIsValidHostile(), GetIsDetectable()
    public AiTarget? GetValidTarget(NwCreature creature, AiTarget? currentTarget)
    public TargetValidity ValidateTarget(NwCreature creature, NwCreature? target)
    public bool IsDetectable(NwCreature creature, NwCreature target)
    public NwCreature? FindNearestEnemy(NwCreature creature, float radius = 30.0f)
}
```

**AiArchetypeService.cs**
```csharp
[ServiceBinding(typeof(AiArchetypeService))]
public class AiArchetypeService
{
    private readonly Dictionary<string, IAiArchetype> _archetypes = new();
    private readonly bool _isEnabled;

    public AiArchetypeService(IEnumerable<IAiArchetype> archetypes)
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";

        if (!_isEnabled) return;

        foreach (var archetype in archetypes)
        {
            _archetypes[archetype.ArchetypeId] = archetype;
        }
    }

    public IAiArchetype? GetArchetype(NwCreature creature)
    {
        // Check creature for archetype marker, fallback to class-based detection
        string archetypeId = creature.GetObjectVariable<LocalVariableString>("ai_archetype").Value;

        if (string.IsNullOrEmpty(archetypeId))
        {
            archetypeId = DetectArchetype(creature);
        }

        return _archetypes.GetValueOrDefault(archetypeId);
    }

    private string DetectArchetype(NwCreature creature)
    {
        // Port GetArchetype() logic from ds_ai_include.nss
        // Returns archetype ID based on class levels (fighter vs caster ratio)
    }
}
```

**AiActivityTrackerService.cs**
```csharp
[ServiceBinding(typeof(AiActivityTrackerService))]
public class AiActivityTrackerService
{
    private readonly Dictionary<uint, AiActivityState> _activityStates = new();
    private readonly bool _isEnabled;

    public AiActivityTrackerService()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public bool IsActive(NwCreature creature)
    {
        if (!_isEnabled) return true;

        if (!_activityStates.TryGetValue(creature.UUID, out var state))
        {
            state = new AiActivityState { LastActivityTime = DateTime.UtcNow };
            _activityStates[creature.UUID] = state;
            return true;
        }

        return (DateTime.UtcNow - state.LastActivityTime).TotalSeconds < 30.0;
    }

    public void MarkActive(NwCreature creature)
    {
        if (!_isEnabled) return;

        if (_activityStates.TryGetValue(creature.UUID, out var state))
        {
            state.LastActivityTime = DateTime.UtcNow;
            state.InactiveHeartbeats = 0;
        }
    }

    public void IncrementInactivity(NwCreature creature)
    {
        if (!_isEnabled) return;

        if (_activityStates.TryGetValue(creature.UUID, out var state))
        {
            state.InactiveHeartbeats++;
        }
    }

    public void Remove(NwCreature creature)
    {
        _activityStates.Remove(creature.UUID);
    }
}
```

#### 1.2 Core Interfaces

**IAiArchetype.cs**
```csharp
public interface IAiArchetype
{
    string ArchetypeId { get; }
    string DisplayName { get; }

    IEnumerable<IAiBehaviorComponent> GetBehaviors();
    int GetPriority(BehaviorContext context);
}
```

**IAiBehaviorComponent.cs**
```csharp
public interface IAiBehaviorComponent
{
    string ComponentId { get; }
    int Priority { get; }

    bool CanExecute(BehaviorContext context);
    BehaviorResult Execute(BehaviorContext context);
}
```

**ITargetingStrategy.cs**
```csharp
public interface ITargetingStrategy
{
    AiTarget? SelectTarget(NwCreature creature, AiTarget? currentTarget);
    bool ShouldSwitchTarget(NwCreature creature, AiTarget currentTarget, AiTarget newTarget);
}
```

**ICombatTactic.cs**
```csharp
public interface ICombatTactic
{
    string TacticId { get; }
    int Priority { get; }

    bool CanExecute(NwCreature creature, AiTarget target);
    TacticResult Execute(NwCreature creature, AiTarget target);
}
```

#### 1.3 Core Models

**CreatureSpellCache.cs**
```csharp
public class CreatureSpellCache
{
    public static readonly CreatureSpellCache Empty = new();

    public int MaxCasterLevel { get; init; }
    public IReadOnlyList<Spell> AttackSpells { get; init; } = Array.Empty<Spell>();
    public IReadOnlyList<Spell> BuffSpells { get; init; } = Array.Empty<Spell>();
    public IReadOnlyList<Spell> HealingSpells { get; init; } = Array.Empty<Spell>();
    public IReadOnlyList<Spell> SummonSpells { get; init; } = Array.Empty<Spell>();
    public Dictionary<Spell, int> SpellUsageCount { get; } = new();

    public bool HasReachedSpamLimit(Spell spell) => SpellUsageCount.GetValueOrDefault(spell) >= 2;
}
```

**AiTarget.cs**
```csharp
public class AiTarget
{
    public NwCreature Creature { get; init; }
    public TargetValidity Validity { get; init; }
    public float Distance { get; init; }
    public DateTime AcquiredAt { get; init; }
    public bool IsPlayerControlled => Creature.IsPlayerControlled;
}

public enum TargetValidity
{
    Invalid = -2,
    Dead = -1,
    NotHostile = 0,
    Undetectable = 1,
    Heard = 2,
    Seen = 3
}
```

**BehaviorContext.cs**
```csharp
public class BehaviorContext
{
    public NwCreature Creature { get; init; }
    public AiTarget? Target { get; init; }
    public IAiArchetype? Archetype { get; init; }
    public CreatureSpellCache SpellCache { get; init; }
    public DateTime Timestamp { get; init; }
}
```

### Phase 2: Basic Archetypes & Components (Week 2)

**Goal**: Implement melee, caster, and hybrid archetypes with basic tactics

#### 2.1 Archetype Implementations

**MeleeArchetype.cs** (Archetype scale 1-3)
```csharp
[ServiceBinding(typeof(IAiArchetype))]
public class MeleeArchetype : IAiArchetype
{
    private readonly AiTalentService _talentService;

    public string ArchetypeId => "melee";
    public string DisplayName => "Melee Fighter";

    public MeleeArchetype(AiTalentService talentService)
    {
        _talentService = talentService;
    }

    public IEnumerable<IAiBehaviorComponent> GetBehaviors()
    {
        yield return new FeatBuffComponent(_talentService);      // Priority 100
        yield return new SpecialAttackComponent(_talentService); // Priority 90
        yield return new MeleeAttackComponent();                 // Priority 80
        yield return new AggressiveMovementComponent();          // Priority 70
    }
}
```

**CasterArchetype.cs** (Archetype scale 7-10)
```csharp
[ServiceBinding(typeof(IAiArchetype))]
public class CasterArchetype : IAiArchetype
{
    public string ArchetypeId => "caster";
    public string DisplayName => "Spellcaster";

    public IEnumerable<IAiBehaviorComponent> GetBehaviors()
    {
        yield return new HealingSpellComponent();     // Priority 100
        yield return new BuffSpellComponent();        // Priority 90
        yield return new SummonSpellComponent();      // Priority 85
        yield return new AttackSpellComponent();      // Priority 80
        yield return new DefensiveMovementComponent(); // Priority 70
    }
}
```

**HybridArchetype.cs** (Archetype scale 4-6)
```csharp
[ServiceBinding(typeof(IAiArchetype))]
public class HybridArchetype : IAiArchetype
{
    public string ArchetypeId => "hybrid";
    public string DisplayName => "Battle Mage";

    public IEnumerable<IAiBehaviorComponent> GetBehaviors()
    {
        yield return new HealingSpellComponent();     // Priority 100
        yield return new BuffSpellComponent();        // Priority 90
        yield return new AttackSpellComponent();      // Priority 85
        yield return new MeleeAttackComponent();      // Priority 80
        yield return new TacticalMovementComponent(); // Priority 70
    }
}
```

#### 2.2 Combat Tactic Implementations

**MeleeAttackTactic.cs**
```csharp
public class MeleeAttackTactic : ICombatTactic
{
    public string TacticId => "melee_attack";
    public int Priority => 80;

    public bool CanExecute(NwCreature creature, AiTarget target)
    {
        return target.Validity >= TargetValidity.Seen;
    }

    public TacticResult Execute(NwCreature creature, AiTarget target)
    {
        // Port DoAttack() logic from ds_ai_include.nss
        // Check for blocking, distance tracking, etc.

        creature.ActionAttack(target.Creature, passive: false);
        return TacticResult.Success;
    }
}
```

**SpellCastingTactic.cs**
```csharp
public class SpellCastingTactic : ICombatTactic
{
    private readonly AiSpellCacheService _spellCache;

    public string TacticId => "spell_attack";
    public int Priority => 80;

    public SpellCastingTactic(AiSpellCacheService spellCache)
    {
        _spellCache = spellCache;
    }

    public bool CanExecute(NwCreature creature, AiTarget target)
    {
        var cache = _spellCache.GetOrCreateCache(creature);
        return cache.AttackSpells.Any() && target.Validity >= TargetValidity.Seen;
    }

    public TacticResult Execute(NwCreature creature, AiTarget target)
    {
        // Port DoSpellCast() logic for attack spells
        var cache = _spellCache.GetOrCreateCache(creature);
        Spell? selectedSpell = SelectBestSpell(cache, target);

        if (selectedSpell == null) return TacticResult.Failure;

        creature.ActionCastSpellAt(selectedSpell.Value, target.Creature);
        cache.SpellUsageCount[selectedSpell.Value] = cache.SpellUsageCount.GetValueOrDefault(selectedSpell.Value) + 1;

        return TacticResult.Success;
    }
}
```

#### 2.3 Targeting Strategy Implementations

**NearestEnemyTargeting.cs**
```csharp
public class NearestEnemyTargeting : ITargetingStrategy
{
    private readonly AiTargetingService _targetingService;

    public NearestEnemyTargeting(AiTargetingService targetingService)
    {
        _targetingService = targetingService;
    }

    public AiTarget? SelectTarget(NwCreature creature, AiTarget? currentTarget)
    {
        // Port GetTarget() logic from ds_ai_include.nss

        // Check if current target is still valid
        if (currentTarget != null)
        {
            var validity = _targetingService.ValidateTarget(creature, currentTarget.Creature);
            if (validity >= TargetValidity.Heard)
            {
                // Check attention span (8 + validity for NPCs, 4 for PCs)
                int attentionSpan = currentTarget.IsPlayerControlled ? 8 + (int)validity : 4;
                if (Random.Shared.Next(1, 13) <= attentionSpan)
                {
                    return currentTarget; // Keep current target
                }
            }
        }

        // Find new target
        NwCreature? newTarget = _targetingService.FindNearestEnemy(creature);
        return newTarget != null ? new AiTarget
        {
            Creature = newTarget,
            Validity = _targetingService.ValidateTarget(creature, newTarget),
            Distance = creature.Distance(newTarget),
            AcquiredAt = DateTime.UtcNow
        } : null;
    }

    public bool ShouldSwitchTarget(NwCreature creature, AiTarget currentTarget, AiTarget newTarget)
    {
        // Prefer PC targets over NPCs
        if (newTarget.IsPlayerControlled && !currentTarget.IsPlayerControlled)
            return true;

        // Switch if current is invalid
        return currentTarget.Validity < TargetValidity.Heard;
    }
}
```

### Phase 3: Event Handler Integration (Week 3)

**Goal**: Wire up archetypes to existing behavior interfaces

#### 3.1 Generic Event Handlers

**GenericOnHeartbeatBehavior.cs**
```csharp
[ServiceBinding(typeof(IOnHeartbeatBehavior))]
public class GenericOnHeartbeatBehavior : IOnHeartbeatBehavior
{
    private readonly AiArchetypeService _archetypeService;
    private readonly AiTargetingService _targetingService;
    private readonly AiActivityTrackerService _activityTracker;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_heartbeat";

    public GenericOnHeartbeatBehavior(
        AiArchetypeService archetypeService,
        AiTargetingService targetingService,
        AiActivityTrackerService activityTracker)
    {
        _archetypeService = archetypeService;
        _targetingService = targetingService;
        _activityTracker = activityTracker;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnHeartbeat(CreatureEvents.OnHeartbeat eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        // Check activity state
        if (!_activityTracker.IsActive(creature)) return;

        // Get archetype
        IAiArchetype? archetype = _archetypeService.GetArchetype(creature);
        if (archetype == null) return;

        // Get or update target
        AiTarget? currentTarget = GetStoredTarget(creature);
        AiTarget? target = _targetingService.GetValidTarget(creature, currentTarget);

        if (target == null)
        {
            _activityTracker.IncrementInactivity(creature);
            return;
        }

        StoreTarget(creature, target);

        // Build behavior context
        var context = new BehaviorContext
        {
            Creature = creature,
            Target = target,
            Archetype = archetype,
            Timestamp = DateTime.UtcNow
        };

        // Execute behavior chain
        bool actionPerformed = false;
        foreach (var behavior in archetype.GetBehaviors().OrderByDescending(b => b.Priority))
        {
            if (behavior.CanExecute(context))
            {
                var result = behavior.Execute(context);
                if (result == BehaviorResult.Success)
                {
                    actionPerformed = true;
                    break;
                }
            }
        }

        if (actionPerformed)
        {
            _activityTracker.MarkActive(creature);
        }
        else
        {
            _activityTracker.IncrementInactivity(creature);
        }
    }

    private AiTarget? GetStoredTarget(NwCreature creature)
    {
        var targetObj = creature.GetObjectVariable<LocalVariableObject<NwCreature>>("ds_ai_target").Value;
        return targetObj != null ? new AiTarget
        {
            Creature = targetObj,
            Distance = creature.Distance(targetObj),
            AcquiredAt = DateTime.UtcNow
        } : null;
    }

    private void StoreTarget(NwCreature creature, AiTarget target)
    {
        creature.GetObjectVariable<LocalVariableObject<NwCreature>>("ds_ai_target").Value = target.Creature;
    }
}
```

**GenericOnDamagedBehavior.cs**
```csharp
[ServiceBinding(typeof(IOnDamagedBehavior))]
public class GenericOnDamagedBehavior : IOnDamagedBehavior
{
    private readonly AiArchetypeService _archetypeService;
    private readonly AiTargetingService _targetingService;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_damaged";

    public GenericOnDamagedBehavior(
        AiArchetypeService archetypeService,
        AiTargetingService targetingService)
    {
        _archetypeService = archetypeService;
        _targetingService = targetingService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnDamaged(CreatureEvents.OnDamaged eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;
        NwGameObject? damager = eventData.DamagedBy;

        if (damager is not NwCreature damagerCreature) return;

        // Get archetype to determine reaction (casters flee, melee stands ground)
        IAiArchetype? archetype = _archetypeService.GetArchetype(creature);
        if (archetype == null) return;

        // Port ds_ai_damaged.nss logic
        // Casters (high archetype) flee when damaged at close range
        // Fighters (low archetype) switch targets based on threat
    }
}
```

**GenericOnSpawnBehavior.cs**
```csharp
[ServiceBinding(typeof(IOnSpawnBehavior))]
public class GenericOnSpawnBehavior : IOnSpawnBehavior
{
    private readonly AiArchetypeService _archetypeService;
    private readonly AiSpellCacheService _spellCache;
    private readonly AiActivityTrackerService _activityTracker;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_spawn";

    public GenericOnSpawnBehavior(
        AiArchetypeService archetypeService,
        AiSpellCacheService spellCache,
        AiActivityTrackerService activityTracker)
    {
        _archetypeService = archetypeService;
        _spellCache = spellCache;
        _activityTracker = activityTracker;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnSpawn(CreatureEvents.OnSpawn eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        // Initialize activity tracking
        _activityTracker.MarkActive(creature);

        // Build spell cache
        _spellCache.GetOrCreateCache(creature);

        // Detect and store archetype
        IAiArchetype? archetype = _archetypeService.GetArchetype(creature);
        if (archetype != null)
        {
            creature.GetObjectVariable<LocalVariableString>("ai_archetype").Value = archetype.ArchetypeId;
        }

        // Apply spawn buffs
        ApplySpawnBuffs(creature, archetype);
    }

    private void ApplySpawnBuffs(NwCreature creature, IAiArchetype? archetype)
    {
        // Port OnSpawnBuff() logic from ds_ai_include.nss
    }
}
```

**GenericOnDeathBehavior.cs**
```csharp
[ServiceBinding(typeof(IOnDeathBehavior))]
public class GenericOnDeathBehavior : IOnDeathBehavior
{
    private readonly AiSpellCacheService _spellCache;
    private readonly AiActivityTrackerService _activityTracker;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_death";

    public GenericOnDeathBehavior(
        AiSpellCacheService spellCache,
        AiActivityTrackerService activityTracker)
    {
        _spellCache = spellCache;
        _activityTracker = activityTracker;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnDeath(CreatureEvents.OnDeath eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        // Clean up cached data
        _spellCache.InvalidateCache(creature);
        _activityTracker.Remove(creature);
    }
}
```

### Phase 4: Boss Mechanics Refactoring (Week 4)

**Goal**: Convert complex boss scripts to state machine patterns

#### 4.1 Phase-Based Boss Base Class

**PhaseBasedAiBehavior.cs**
```csharp
public abstract class PhaseBasedAiBehavior
{
    protected abstract IReadOnlyList<BossPhase> Phases { get; }

    protected int GetCurrentPhase(NwCreature boss)
    {
        return boss.GetObjectVariable<LocalVariableInt>("boss_current_phase").Value;
    }

    protected void SetCurrentPhase(NwCreature boss, int phase)
    {
        boss.GetObjectVariable<LocalVariableInt>("boss_current_phase").Value = phase;
    }

    protected bool HasPhaseTriggered(NwCreature boss, int phase)
    {
        return boss.GetObjectVariable<LocalVariableInt>($"boss_phase_{phase}_triggered").Value == 1;
    }

    protected void MarkPhaseTriggered(NwCreature boss, int phase)
    {
        boss.GetObjectVariable<LocalVariableInt>($"boss_phase_{phase}_triggered").Value = 1;
    }

    protected void CheckPhaseTransitions(NwCreature boss)
    {
        float hpPercent = (float)boss.HP / boss.MaxHP * 100f;

        foreach (var phase in Phases)
        {
            if (hpPercent <= phase.TriggerHpPercent && !HasPhaseTriggered(boss, phase.PhaseNumber))
            {
                ExecutePhase(boss, phase);
                MarkPhaseTriggered(boss, phase.PhaseNumber);
                SetCurrentPhase(boss, phase.PhaseNumber);
                break;
            }
        }
    }

    protected abstract void ExecutePhase(NwCreature boss, BossPhase phase);
}

public record BossPhase
{
    public int PhaseNumber { get; init; }
    public float TriggerHpPercent { get; init; }
    public string Description { get; init; } = string.Empty;
    public Action<NwCreature>? OnEnter { get; init; }
    public Action<NwCreature>? OnExit { get; init; }
}
```

#### 4.2 Necromancer Lich Boss Implementation

**NecroLichBossAi.cs** (Replaces `ds_ai2_necroboss.nss`)
```csharp
[ServiceBinding(typeof(IOnDamagedBehavior))]
public class NecroLichBossAi : PhaseBasedAiBehavior, IOnDamagedBehavior
{
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai2_necroboss";

    public NecroLichBossAi()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    protected override IReadOnlyList<BossPhase> Phases => new[]
    {
        new BossPhase
        {
            PhaseNumber = 1,
            TriggerHpPercent = 90,
            Description = "Summon skeletal warriors",
            OnEnter = boss => SummonArmy(boss, armyType: 1)
        },
        new BossPhase
        {
            PhaseNumber = 2,
            TriggerHpPercent = 80,
            Description = "Cast epic wail + summon archers",
            OnEnter = boss =>
            {
                ApplyInvulnerability(boss, duration: 10.0f);
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => SummonArmy(boss, armyType: 1));
            }
        },
        new BossPhase
        {
            PhaseNumber = 3,
            TriggerHpPercent = 70,
            Description = "Teleport + summon minions",
            OnEnter = boss =>
            {
                TeleportToRandomSpot(boss);
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => SummonArmy(boss, armyType: 1));
            }
        },
        new BossPhase
        {
            PhaseNumber = 4,
            TriggerHpPercent = 60,
            Description = "Summon mages",
            OnEnter = boss =>
            {
                ApplyInvulnerability(boss, duration: 10.0f);
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => SummonArmy(boss, armyType: 1));
            }
        },
        new BossPhase
        {
            PhaseNumber = 5,
            TriggerHpPercent = 50,
            Description = "Epic death spell + full heal + lock doors",
            OnEnter = boss =>
            {
                LockDoors(boss);
                ApplyInvulnerability(boss, duration: 10.0f);
                boss.SpeakString("I will tear your souls from your chests! *Summons forth an epic spell of death filling the arena");
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => CastDeathHealSpell(boss));
                boss.DelayCommand(TimeSpan.FromSeconds(6), () => boss.ApplyEffect(EffectDuration.Instant, Effect.Heal(9999)));
            }
        },
        new BossPhase
        {
            PhaseNumber = 6,
            TriggerHpPercent = 40,
            Description = "Summon dragons",
            OnEnter = boss =>
            {
                ApplyInvulnerability(boss, duration: 10.0f);
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => SummonArmy(boss, armyType: 3));
            }
        },
        new BossPhase
        {
            PhaseNumber = 7,
            TriggerHpPercent = 30,
            Description = "Epic negative energy spell",
            OnEnter = boss =>
            {
                ApplyInvulnerability(boss, duration: 10.0f);
                boss.SpeakString("Impressive, still alive? Hmm. Not for long. *Summons forth an epic negative energy spell");
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => CastNegativeDeathSpell(boss));
            }
        },
        new BossPhase
        {
            PhaseNumber = 8,
            TriggerHpPercent = 20,
            Description = "Summon tough melee",
            OnEnter = boss =>
            {
                ApplyInvulnerability(boss, duration: 10.0f);
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => SummonArmy(boss, armyType: 2));
            }
        },
        new BossPhase
        {
            PhaseNumber = 9,
            TriggerHpPercent = 10,
            Description = "Epic fire spell",
            OnEnter = boss =>
            {
                ApplyInvulnerability(boss, duration: 10.0f);
                boss.SpeakString("This is the end. *Once more summons forth a massive epic evocation spell of fire*");
                boss.DelayCommand(TimeSpan.FromSeconds(5), () => CastEvocationFireSpell(boss));
            }
        },
        new BossPhase
        {
            PhaseNumber = 10,
            TriggerHpPercent = 1,
            Description = "Death and loot drop",
            OnEnter = boss =>
            {
                boss.SpeakString("You think this is the end!? I will be reborn again and again... *The Lich's current body is destroyed*");
                boss.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfGasExplosionMind));
                DropLoot(boss);
                boss.DelayCommand(TimeSpan.FromSeconds(1), () => boss.Destroy());
            }
        }
    };

    public void OnDamaged(CreatureEvents.OnDamaged eventData)
    {
        if (!_isEnabled) return;

        NwCreature boss = eventData.Creature;

        // Only process for necro boss
        if (boss.ResRef != "necroboss") return;

        // Check for phase transitions
        CheckPhaseTransitions(boss);

        // Random teleport chance (replaces nRandom == 0 check)
        if (Random.Shared.Next(12) == 0)
        {
            OnHitTeleport(eventData.DamagedBy as NwCreature);
        }

        // Standard damaged behavior (port from ds_ai2_necroboss.nss)
        HandleStandardDamageReaction(boss, eventData.DamagedBy);
    }

    protected override void ExecutePhase(NwCreature boss, BossPhase phase)
    {
        phase.OnEnter?.Invoke(boss);
    }

    private void SummonArmy(NwCreature boss, int armyType)
    {
        // Port summon logic from ds_ai2_necroboss.nss
        // Create creatures at waypoint locations based on armyType
    }

    private void ApplyInvulnerability(NwCreature boss, float duration)
    {
        boss.PlotFlag = true;
        TeleportToRandomSpot(boss);
        boss.DelayCommand(TimeSpan.FromSeconds(duration), () => boss.PlotFlag = false);
    }

    private void TeleportToRandomSpot(NwCreature boss)
    {
        int randomSpot = Random.Shared.Next(1, 5);
        var waypoint = NwObject.FindObjectsWithTag<NwWaypoint>($"necromagic{randomSpot}").FirstOrDefault();
        if (waypoint != null)
        {
            boss.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(VfxType.FnfSummonMonster1));
            boss.ClearActionQueue();
            boss.JumpToObject(waypoint);
        }
    }

    // ... Additional helper methods for spells, summoning, etc.
}
```

#### 4.3 Purple Worm Boss Implementation

**PurpleWormAi.cs** (Replaces `ds_ai2_heartworm.nss`, `ds_ai2_deathworm.nss`, `ds_ai2_dampworm.nss`)
```csharp
[ServiceBinding(typeof(IOnHeartbeatBehavior))]
[ServiceBinding(typeof(IOnDamagedBehavior))]
public class PurpleWormAi : IOnHeartbeatBehavior, IOnDamagedBehavior
{
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai2_heartworm";

    public PurpleWormAi()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnHeartbeat(CreatureEvents.OnHeartbeat eventData)
    {
        if (!_isEnabled) return;

        NwCreature worm = eventData.Creature;

        // Port heartbeat logic from ds_ai2_heartworm.nss
        // Check for enemies in range, AoE timer, etc.

        NwCreature? nearestEnemy = worm.GetNearestCreature(CreatureTypeFilter.Reputation(ReputationType.Enemy));
        float distance = nearestEnemy?.Distance(worm) ?? float.MaxValue;

        // AoE attack if no melee range enemies
        int aoeTimer = worm.GetObjectVariable<LocalVariableInt>("aoeTimer").Value;
        int aoeFired = worm.GetObjectVariable<LocalVariableInt>("aoeFired").Value;

        if (aoeFired >= 1)
        {
            worm.GetObjectVariable<LocalVariableInt>("aoeFired").Value = 0;
        }
        else if (distance > 0 && distance < 4.0f)
        {
            worm.GetObjectVariable<LocalVariableInt>("aoeTimer").Value = 0;
        }
        else if (aoeTimer >= 2)
        {
            LaunchAoEDamage(worm);
            worm.GetObjectVariable<LocalVariableInt>("aoeFired").Value = 1;
        }
        else
        {
            worm.GetObjectVariable<LocalVariableInt>("aoeTimer").Value = aoeTimer + 1;
        }
    }

    public void OnDamaged(CreatureEvents.OnDamaged eventData)
    {
        if (!_isEnabled) return;

        // Port damaged logic from ds_ai2_dampworm.nss
    }

    private void LaunchAoEDamage(NwCreature worm)
    {
        // Port AoE damage logic
        worm.SpeakString("*The worm burrows rapidly, causing the ground to shake violently*");

        foreach (var creature in worm.GetNearestCreatures())
        {
            if (creature.Distance(worm) <= 30.0f)
            {
                // Apply damage and knockdown
            }
        }
    }
}
```

### Phase 5: Performance Optimization & Polish (Week 5)

**Goal**: Optimize for production-scale performance

#### 5.1 Optimization Strategies

1. **Object Pooling for Contexts**
```csharp
public class BehaviorContextPool
{
    private readonly ConcurrentBag<BehaviorContext> _pool = new();

    public BehaviorContext Rent()
    {
        return _pool.TryTake(out var context) ? context : new BehaviorContext();
    }

    public void Return(BehaviorContext context)
    {
        _pool.Add(context);
    }
}
```

2. **Batch Processing for Inactive Creatures**
```csharp
public class AiActivityTrackerService
{
    private readonly Timer _cleanupTimer;

    public AiActivityTrackerService()
    {
        _cleanupTimer = new Timer(CleanupInactiveCreatures, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    private void CleanupInactiveCreatures(object? state)
    {
        var cutoff = DateTime.UtcNow - TimeSpan.FromMinutes(10);
        var toRemove = _activityStates.Where(kvp => kvp.Value.LastActivityTime < cutoff).ToList();

        foreach (var (id, _) in toRemove)
        {
            _activityStates.Remove(id);
        }
    }
}
```

3. **Spell Cache Warm-up**
```csharp
public class AiSpellCacheService
{
    public void WarmCache(NwCreature creature)
    {
        // Pre-build cache during spawn to avoid heartbeat lag
        Task.Run(() => GetOrCreateCache(creature));
    }
}
```

#### 5.2 Monitoring & Telemetry

**AiPerformanceMonitor.cs**
```csharp
[ServiceBinding(typeof(AiPerformanceMonitor))]
public class AiPerformanceMonitor
{
    private readonly Dictionary<string, AiMetrics> _metrics = new();
    private readonly bool _isEnabled;

    public AiPerformanceMonitor()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void RecordBehaviorExecution(string behaviorId, TimeSpan duration, bool success)
    {
        if (!_isEnabled) return;

        if (!_metrics.TryGetValue(behaviorId, out var metrics))
        {
            metrics = new AiMetrics();
            _metrics[behaviorId] = metrics;
        }

        metrics.TotalExecutions++;
        metrics.TotalDuration += duration;
        if (success) metrics.SuccessfulExecutions++;
    }

    public void DumpMetrics()
    {
        if (!_isEnabled) return;

        foreach (var (behaviorId, metrics) in _metrics.OrderByDescending(kvp => kvp.Value.TotalDuration))
        {
            NWScript.WriteTimestampedLogEntry($"[AI Metrics] {behaviorId}: " +
                $"{metrics.TotalExecutions} executions, " +
                $"{metrics.SuccessfulExecutions} successes, " +
                $"Avg: {metrics.AverageDuration.TotalMilliseconds:F2}ms");
        }
    }
}

public class AiMetrics
{
    public int TotalExecutions { get; set; }
    public int SuccessfulExecutions { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan AverageDuration => TotalExecutions > 0
        ? TimeSpan.FromTicks(TotalDuration.Ticks / TotalExecutions)
        : TimeSpan.Zero;
}
```

---

## Migration Strategy

### Parallel Operation Mode

During development and testing, both systems operate in parallel:

1. **Legacy System** - Continues to handle all creatures on `live` server
2. **New System** - Active only on `dev` and `test` servers via feature flag

### Testing Checklist

- [ ] Melee archetype creatures engage in combat properly
- [ ] Caster archetype creatures cast appropriate spells
- [ ] Hybrid archetype creatures balance melee and spells
- [ ] Spell caching doesn't cause memory leaks
- [ ] Inactive creature tracking reduces CPU usage
- [ ] Boss phase transitions trigger at correct HP thresholds
- [ ] Necro Lich boss executes all 10 phases correctly
- [ ] Purple Worm AoE attack triggers when players stay at range
- [ ] Target switching respects attention span rules
- [ ] PC targets preferred over NPC targets
- [ ] No performance degradation with 50+ AI creatures active

### Rollout Plan

1. **Phase 1-2 (Weeks 1-2)**: Core infrastructure on `dev` only
2. **Phase 3 (Week 3)**: Event handlers tested on `test` server
3. **Phase 4 (Week 4)**: Boss mechanics validated with player groups
4. **Phase 5 (Week 5)**: Performance testing under load
5. **Production Cutover**: Feature flag switched to enable on `live`

---

## Performance Benchmarks

### Target Metrics

| Metric | Target | Current Legacy | New System Goal |
|--------|--------|----------------|-----------------|
| Heartbeat processing | <5ms per creature | ~8ms | <3ms |
| Spell cache lookup | <1ms | N/A (rebuilds) | <0.5ms |
| Target selection | <2ms | ~4ms | <1ms |
| Inactive creature overhead | 0ms | ~2ms | 0ms (skipped) |
| Memory per creature | <100 KB | ~80 KB | <50 KB |

### Load Testing Scenarios

1. **100 Melee Creatures** - Stress test basic attack loops
2. **50 Caster Creatures** - Spell cache performance
3. **10 Boss Encounters** - Phase transition timing
4. **Mixed 200 Creatures** - Real-world scenario

---

## Code Quality Standards

### Required Patterns

✅ **Dependency Injection** - All services use `[ServiceBinding]`
✅ **Feature Flags** - Check `SERVER_MODE` in constructors
✅ **Interface Segregation** - Small, focused interfaces
✅ **Immutable Models** - Use `record` and `init` properties
✅ **Extension Methods** - Keep core types clean

### Forbidden Patterns

❌ **String-based local variables** - Use typed `LocalVariable<T>`
❌ **Magic numbers** - Use named constants
❌ **Deep inheritance** - Prefer composition
❌ **Singletons** - Use DI container
❌ **Tight coupling** - Depend on interfaces

---

## Future Enhancements

### Post-Launch Improvements

1. **Behavior Trees** - Visual editor for complex AI sequences
2. **Machine Learning** - Adaptive difficulty based on player performance
3. **Designer Tools** - JSON-based archetype configuration without code changes
4. **Replay System** - Record and analyze AI decisions for debugging
5. **Dynamic Archetypes** - Creatures evolve behavior based on combat history

### Integration Opportunities

- **WorldSimulator** - AI decisions influence economy (bandits raid trade routes)
- **DMS System** - DM-controllable AI parameters in real-time
- **Event Bus** - AI publishes domain events for analytics

---

## Success Criteria

The refactoring is considered successful when:

✅ All legacy `.nss` scripts have C# equivalents
✅ Performance metrics meet or exceed targets
✅ No critical bugs in 2 weeks of `test` server operation
✅ DM staff approve behavior quality in boss encounters
✅ Player feedback confirms no regression in AI challenge
✅ Code coverage >80% for core services
✅ Documentation complete and approved

---

## Contacts & Resources

**Lead Developer**: [Your Name]
**Architecture Review**: [Reviewer Name]
**QA Lead**: [QA Name]

**Documentation**:
- [Anvil API Documentation](https://nwn.niv.gg/)
- [AgentsInstructions.md](../AgentsInstructions.md)
- [NWN Lexicon](https://nwnlexicon.com/)

**Repository**: `/AmiaReforged.PwEngine/Features/AI/`

---

*Last Updated: November 22, 2025*

