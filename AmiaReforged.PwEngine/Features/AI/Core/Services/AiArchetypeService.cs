using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Interfaces;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Detects and manages creature archetypes based on class levels.
/// Ports logic from ds_ai_include.nss lines 1329-1391.
/// </summary>
[ServiceBinding(typeof(AiArchetypeService))]
public class AiArchetypeService
{
    private readonly Dictionary<string, IAiArchetype> _archetypes = new();
    private readonly AiStateManager _stateManager;
    private readonly bool _isEnabled;

    public AiArchetypeService(
        IEnumerable<IAiArchetype> archetypes,
        AiStateManager stateManager)
    {
        _stateManager = stateManager;
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";

        if (!_isEnabled) return;

        // Register all archetype implementations
        foreach (IAiArchetype archetype in archetypes)
        {
            _archetypes[archetype.ArchetypeId] = archetype;
        }
    }

    /// <summary>
    /// Gets the archetype for a creature, detecting it from class levels if needed.
    /// </summary>
    public IAiArchetype? GetArchetype(NwCreature creature)
    {
        if (!_isEnabled) return null;

        AiState state = _stateManager.GetOrCreateState(creature);

        // Check if archetype already assigned
        if (!string.IsNullOrEmpty(state.ArchetypeId))
        {
            return _archetypes.GetValueOrDefault(state.ArchetypeId);
        }

        // Detect archetype from class levels
        int archetypeValue = DetectArchetype(creature);
        string archetypeId = archetypeValue switch
        {
            <= 3 => "melee",
            <= 6 => "hybrid",
            _ => "caster"
        };

        state.ArchetypeId = archetypeId;
        return _archetypes.GetValueOrDefault(archetypeId);
    }

    /// <summary>
    /// Detects archetype value (1-10) based on class levels.
    /// Port of GetArchetype() from ds_ai_include.nss lines 1329-1366.
    /// </summary>
    private int DetectArchetype(NwCreature creature)
    {
        int totalLevels = 0;
        int weightedLevels = 0;

        // Process up to 3 classes
        for (int i = 0; i < 3; i++)
        {
            CreatureClassInfo? classInfo = creature.Classes.ElementAtOrDefault(i);
            if (classInfo == null) break;

            int levels = classInfo.Level;
            int factor = GetClassWeightingFactor(classInfo.Class.ClassType);

            totalLevels += levels;
            weightedLevels += levels * factor;
        }

        if (totalLevels == 0) return 1; // Default to melee

        // Formula: (weightedLevels / totalLevels) * 10 / 3, clamped to 1-10
        // This produces values where:
        // - Pure martial (factor 1): 1 * 10 / 3 = 3.33 → 3
        // - Pure hybrid (factor 2): 2 * 10 / 3 = 6.66 → 6
        // - Pure caster (factor 3): 3 * 10 / 3 = 10 → 10
        float ratio = (float)weightedLevels / totalLevels;
        int archetype = (int)Math.Ceiling(ratio * 10.0f / 3.0f);
        return Math.Clamp(archetype, 1, 10);
    }

    /// <summary>
    /// Gets the class weighting factor for archetype calculation.
    /// Port of GetClassFactor() from ds_ai_include.nss lines 1368-1391.
    /// </summary>
    private int GetClassWeightingFactor(ClassType classType)
    {
        return classType switch
        {
            // Pure martial classes (factor 1)
            ClassType.Fighter => 1,
            ClassType.Barbarian => 1,
            ClassType.Ranger => 1,
            ClassType.Rogue => 1,

            // Hybrid classes (factor 2)
            ClassType.Paladin => 2,
            ClassType.Monk => 2,
            ClassType.Druid => 2,
            ClassType.Cleric => 2,

            // Full caster classes (factor 3)
            ClassType.Wizard => 3,
            ClassType.Sorcerer => 3,
            ClassType.Bard => 3,

            // Prestige/other classes default to martial
            _ => 1
        };
    }
}
