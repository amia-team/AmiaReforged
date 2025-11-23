using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Behaviors;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI spawn handler that initializes AI state, builds spell cache, and applies feat buffs.
/// Ports logic from ds_ai_spawn.nss.
/// </summary>
[ServiceBinding(typeof(IOnSpawnBehavior))]
public class GenericAiSpawn : IOnSpawnBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiSpellCacheService _spellCacheService;
    private readonly AiTalentService _talentService;
    private readonly AiArchetypeService _archetypeService;
    private readonly bool _isEnabled;

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

        var creature = eventData.Creature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        // Initialize AI state
        var state = _stateManager.GetOrCreateState(creature);

        // Delayed initialization (1 second) to allow creature to fully spawn
        NWScript.DelayCommand(1.0f, () =>
        {
            // Apply feat buffs (Rage, Divine Shield, etc.)
            _talentService.TryUseFeatBuff(creature);
        });

        // Build spell cache
        _spellCacheService.GetOrCreateCache(creature);

        // Detect and cache archetype
        _archetypeService.GetArchetype(creature);

        // Set AI identifier for compatibility with legacy scripts
        creature.GetObjectVariable<LocalVariableString>("ai").Value = "ds_ai";
    }
}
