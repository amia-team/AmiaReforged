using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Typed AI state for a creature, managed by AiStateManager.
/// Replaces local variables with proper lifecycle management.
/// </summary>
public class AiState
{
    /// <summary>
    /// The creature UUID this state belongs to.
    /// </summary>
    public uint CreatureId { get; init; }

    /// <summary>
    /// Current target being engaged (creature, placeable, etc.).
    /// </summary>
    public NwGameObject? CurrentTarget { get; set; }

    /// <summary>
    /// Last target that damaged this creature (for target switching).
    /// </summary>
    public NwCreature? LastDamager { get; set; }

    /// <summary>
    /// Archetype ID assigned to this creature ("melee", "caster", "hybrid").
    /// </summary>
    public string? ArchetypeId { get; set; }

    /// <summary>
    /// Last spell cast by this creature (prevents immediate re-casting of buffs).
    /// </summary>
    public Spell? LastSpellCast { get; set; }

    /// <summary>
    /// Whether feat buffs have been applied on spawn (one-time).
    /// </summary>
    public bool HasFeatBuffed { get; set; }

    /// <summary>
    /// Number of consecutive inactive heartbeats (for sleep mode).
    /// </summary>
    public int InactiveHeartbeats { get; set; }

    /// <summary>
    /// Last time this creature performed an action.
    /// </summary>
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last recorded distance to target (for blocking detection).
    /// </summary>
    public float LastDistance { get; set; }

    /// <summary>
    /// Creature that is blocking this creature's path.
    /// </summary>
    public NwCreature? BlockedBy { get; set; }

    /// <summary>
    /// Whether this creature is in sleep mode (inactive >5 heartbeats).
    /// </summary>
    public bool IsSleeping => InactiveHeartbeats > 5;

    /// <summary>
    /// Whether DMs should be warned about extended inactivity (>100 heartbeats = 10 minutes).
    /// </summary>
    public bool ShouldWarnDm => InactiveHeartbeats >= 100;

    /// <summary>
    /// Marks this creature as active (resets counters).
    /// </summary>
    public void MarkActive()
    {
        InactiveHeartbeats = 1;
        LastActivityTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Increments the inactive heartbeat counter.
    /// </summary>
    public void IncrementInactivity()
    {
        InactiveHeartbeats++;
    }
}

