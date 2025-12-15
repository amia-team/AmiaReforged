using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Behaviors;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors.Generic;

/// <summary>
/// Generic AI death handler that coordinates all death-related logic.
/// Ports and consolidates logic from ds_ai_death.nss and inc_ds_ondeath.nss.
///
/// Responsibilities:
/// - AI state and cache cleanup
/// - XP and gold reward distribution (via IXpRewardHandler)
/// - Loot generation (via ILootGenerator)
/// </summary>
[ServiceBinding(typeof(IOnDeathBehavior))]
public class GenericAiDeath : IOnDeathBehavior
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly AiStateManager _stateManager;
    private readonly AiSpellCacheService _spellCacheService;
    private readonly IXpRewardHandler _xpRewardHandler;
    private readonly ILootGenerator _lootGenerator;
    private readonly ICreatureClassifier _classifier;
    private readonly bool _isEnabled;

    public string ScriptName => "ds_ai_death";

    public GenericAiDeath(
        AiStateManager stateManager,
        AiSpellCacheService spellCacheService,
        IXpRewardHandler xpRewardHandler,
        ILootGenerator lootGenerator,
        ICreatureClassifier classifier)
    {
        _stateManager = stateManager;
        _spellCacheService = spellCacheService;
        _xpRewardHandler = xpRewardHandler;
        _lootGenerator = lootGenerator;
        _classifier = classifier;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    public void OnDeath(CreatureEvents.OnDeath eventData)
    {
        if (!_isEnabled) return;

        NwCreature creature = eventData.KilledCreature;
        NwObject? killer = eventData.Killer;
        if(killer is not NwGameObject killerCreature)
        {
            return;
        }

        // Skip player-controlled creatures
        if (creature.IsPlayerControlled || creature.IsDMAvatar) return;

        // Cleanup AI state and caches to prevent memory leaks
        CleanupAiState(creature);

        // Process rewards and loot for NPC deaths
        CreatureClassification classification = _classifier.Classify(creature);
        if (classification == CreatureClassification.Npc)
        {
            ProcessDeathRewards(creature, killerCreature);
        }
    }

    /// <summary>
    /// Cleans up AI state and caches for a dead creature.
    /// </summary>
    private void CleanupAiState(NwCreature creature)
    {
        _stateManager.RemoveState(creature);
        _spellCacheService.InvalidateCache(creature);
    }

    /// <summary>
    /// Processes XP rewards and loot generation for a killed NPC.
    /// </summary>
    private void ProcessDeathRewards(NwCreature killedCreature, NwGameObject? killer)
    {
        if (killer == null) return;

        try
        {
            // Calculate and distribute XP/gold rewards
            XpRewardResult xpResult = _xpRewardHandler.CalculateAndDistributeRewards(killedCreature, killer);

            // Generate loot based on XP result
            LootGenerationResult lootResult = _lootGenerator.GenerateLoot(killedCreature, killer, xpResult);

            // Log for debugging
            if (lootResult.LootGenerated)
            {
                Log.Debug($"Generated {lootResult.GeneratedItems.Count} items for {killedCreature.Name} " +
                         $"(Tier: {lootResult.Tier}, Mythal: {lootResult.DroppedMythal})");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error processing death rewards for {killedCreature.Name}");
        }
    }
}

