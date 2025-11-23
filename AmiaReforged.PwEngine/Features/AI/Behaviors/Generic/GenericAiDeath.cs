using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Behaviors;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI death handler that cleans up AI state and caches.
/// Ports logic from ds_ai_death.nss.
/// Note: XP rewards and loot generation are handled by inc_ds_ondeath separately.
/// </summary>
[ServiceBinding(typeof(IOnDeathBehavior))]
public class GenericAiDeath : IOnDeathBehavior
{
    private readonly AiStateManager _stateManager;
    private readonly AiSpellCacheService _spellCacheService;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_death";

    public GenericAiDeath(
        AiStateManager stateManager,
        AiSpellCacheService spellCacheService)
    {
        _stateManager = stateManager;
        _spellCacheService = spellCacheService;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnDeath(CreatureEvents.OnDeath eventData)
    {
        if (!_isEnabled) return;

        var creature = eventData.KilledCreature;

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        // Cleanup AI state and caches to prevent memory leaks
        _stateManager.RemoveState(creature);
        _spellCacheService.InvalidateCache(creature);

        // Note: The existing death logic (RewardXPForKill, GenerateLoot)
        // is handled by inc_ds_ondeath which is likely registered separately
    }
}

