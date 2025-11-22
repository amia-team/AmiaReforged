using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Manages AI state lifecycle for creatures.
/// Replaces local variable storage with typed, in-memory state management.
/// State is created on-demand and cleaned up via OnDeath handlers.
/// </summary>
[ServiceBinding(typeof(AiStateManager))]
public class AiStateManager
{
    private readonly Dictionary<uint, AiState> _states = new();
    private readonly bool _isEnabled;

    public AiStateManager()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    /// <summary>
    /// Gets the AI state for a creature, or null if not managed.
    /// </summary>
    public AiState? GetState(NwCreature creature)
    {
        if (!_isEnabled) return null;
        return _states.GetValueOrDefault(creature);
    }

    /// <summary>
    /// Gets or creates AI state for a creature.
    /// Creates new state if it doesn't exist.
    /// </summary>
    public AiState GetOrCreateState(NwCreature creature)
    {
        if (!_isEnabled) return CreateEmptyState(creature);

        if (!_states.TryGetValue(creature, out var state))
        {
            state = CreateStateFromCreature(creature);
            _states[creature] = state;
        }

        return state;
    }

    /// <summary>
    /// Removes AI state for a creature (called on death).
    /// </summary>
    public void RemoveState(NwCreature creature)
    {
        if (!_isEnabled) return;
        _states.Remove(creature);
    }

    /// <summary>
    /// Gets the count of managed creature states (for diagnostics).
    /// </summary>
    public int GetManagedCreatureCount()
    {
        return _isEnabled ? _states.Count : 0;
    }

    private AiState CreateStateFromCreature(NwCreature creature)
    {
        // Create new state, optionally migrating from legacy local variables
        var state = new AiState
        {
            CreatureId = creature
        };

        // Migrate from legacy local variables if they exist (for backward compatibility)
        var legacyArchetype = creature.GetObjectVariable<LocalVariableString>("ds_ai_archetype").Value;
        if (!string.IsNullOrEmpty(legacyArchetype))
        {
            state.ArchetypeId = legacyArchetype;
        }

        var legacyInactive = creature.GetObjectVariable<LocalVariableInt>("ds_ai_i").Value;
        if (legacyInactive > 0)
        {
            state.InactiveHeartbeats = legacyInactive;
        }

        return state;
    }

    private AiState CreateEmptyState(NwCreature creature)
    {
        // Create a temporary state for when system is disabled
        return new AiState { CreatureId = creature };
    }
}
