using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Behaviors;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI spawn handler that initializes AI state, builds spell cache, and applies feat buffs.
/// Ports logic from ds_ai_spawn.nss and OnSpawnRoutines() from ds_ai_include.nss.
///
/// Encapsulates:
/// - AI state initialization and archetype detection
/// - Spell cache building
/// - Feat buffs (Rage, Divine Shield, etc.)
/// - Sneak archetype: starts stealthed if Hide/Move Silently > HD
/// - HiPS archetype: enters stealth on spawn
/// - Silent shout listening (pattern 1001 for M_ATTACKED ally response)
/// - True Seeing from hide: scans equipped hide for ITEM_PROPERTY_TRUE_SEEING
/// - OnSpawn ability at index 100 (SpellID_100 / AbilityID_100)
/// </summary>
[ServiceBinding(typeof(IOnSpawnBehavior))]
public class GenericAiSpawn : IOnSpawnBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiSpellCacheService _spellCacheService;
    private readonly AiTalentService _talentService;
    private readonly AiArchetypeService _archetypeService;
    private readonly bool _isEnabled;

    /// <summary>
    /// Listen pattern for the "ally attacked" silent shout.
    /// </summary>
    private const int ShoutPatternAttacked = 1001;

    /// <summary>
    /// Silent shout message matching legacy M_ATTACKED constant.
    /// </summary>
    private const string AttackedShout = "ds_ai_attacked";

    public string ScriptName => "ds_ai_spawn";

    public GenericAiSpawn(
        AiStateManager stateManager,
        AiSpellCacheService spellCacheService,
        AiTalentService talentService,
        AiArchetypeService archetypeService)
    {
        _stateManager = stateManager;
        _spellCacheService = spellCacheService;
        _talentService = talentService;
        _archetypeService = archetypeService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnSpawn(CreatureEvents.OnSpawn eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.Creature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        // Initialize AI state
        AiState state = _stateManager.GetOrCreateState(creature);

        // Build spell cache
        _spellCacheService.GetOrCreateCache(creature);

        // Detect and cache archetype (includes sneak/hips/ranged detection)
        _archetypeService.GetArchetype(creature);

        // Set AI identifier for compatibility with legacy scripts
        creature.GetObjectVariable<LocalVariableString>("ai").Value = "ds_ai";

        // Enable silent shout listening for ally-attacked pattern (1001)
        NWScript.SetListening(creature, NWScript.TRUE);
        NWScript.SetListenPattern(creature, AttackedShout, ShoutPatternAttacked);

        // Check for True Seeing from equipped hide
        ApplyTrueSeeingFromHide(creature);

        // Delayed initialization (1 second) to allow creature to fully spawn
        NWScript.DelayCommand(1.0f, () =>
        {
            // Apply feat buffs (Rage, Divine Shield, etc.)
            _talentService.TryUseFeatBuff(creature);

            // Sneak/HiPS archetype: start stealthed
            if (_archetypeService.IsSneakArchetype(creature) ||
                _archetypeService.IsHipsArchetype(creature))
            {
                NWScript.SetActionMode(creature, NWScript.ACTION_MODE_STEALTH, 1);
            }
        });

        // Process OnSpawn ability at index 100 (SpellID_100 / AbilityID_100)
        ProcessSpawnAbility(creature);

        // Find nearest hostile and attack on spawn (non-casters only)
        List<NwCreature> nearestCreatures =
            creature.GetNearestCreatures(CreatureTypeFilter.Perception(PerceptionType.SeenAndHeard))
                .Where(c => c.IsEnemy(creature)).ToList();

        bool isCaster = _archetypeService.IsCasterArchetype(creature);

        if (isCaster || nearestCreatures.Count <= 0) return;

        state.MarkActive();

        if (nearestCreatures.Count > 0)
        {
            creature.ActionAttackTarget(nearestCreatures.First());
        }
    }

    /// <summary>
    /// Checks creature's equipped hide for ITEM_PROPERTY_TRUE_SEEING and applies
    /// permanent supernatural True Seeing effect if found.
    /// Ports ds_ai2_spawn.nss True Seeing from hide logic.
    /// </summary>
    private void ApplyTrueSeeingFromHide(NwCreature creature)
    {
        NwItem? hide = creature.GetItemInSlot(InventorySlot.CreatureSkin);
        if (hide == null) return;

        bool hasTrueSeeing = hide.ItemProperties
            .Any(ip => ip.Property.PropertyType == ItemPropertyType.TrueSeeing);

        if (hasTrueSeeing)
        {
            Effect trueSeeing = Effect.TrueSeeing();
            trueSeeing.SubType = EffectSubType.Supernatural;
            creature.ApplyEffect(EffectDuration.Permanent, trueSeeing);
        }
    }

    /// <summary>
    /// Processes OnSpawn ability trigger at index 100.
    /// Ports ds_ai_include.nss SetOnSpawnEffects() lines for SpellID_100 / AbilityID_100:
    /// - SpellID_100: cheat-casts the spell on spawn with 4s AI override
    /// - AbilityID_100: executes named script on spawn with 4s AI override
    /// </summary>
    private void ProcessSpawnAbility(NwCreature creature)
    {
        int spawnSpell = creature.GetObjectVariable<LocalVariableInt>("SpellID_100").Value;
        if (spawnSpell != 0)
        {
            creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Value = 1;
            NWScript.DelayCommand(4.0f,
                () => creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Delete());

            NWScript.ActionCastSpellAtObject(spawnSpell, creature, (int)MetaMagic.Any, 1, 0, 0, 1);

            creature.GetObjectVariable<LocalVariableInt>("SpellID_100").Delete();
            return;
        }

        string spawnAbility =
            creature.GetObjectVariable<LocalVariableString>("AbilityID_100").Value ?? "";
        if (!string.IsNullOrEmpty(spawnAbility))
        {
            creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Value = 1;
            NWScript.DelayCommand(4.0f,
                () => creature.GetObjectVariable<LocalVariableInt>("OverrideAI").Delete());

            creature.GetObjectVariable<LocalVariableObject<NwGameObject>>(spawnAbility).Value = creature;
            NWScript.ExecuteScript(spawnAbility, creature);

            creature.GetObjectVariable<LocalVariableString>("AbilityID_100").Delete();
        }
    }
}
