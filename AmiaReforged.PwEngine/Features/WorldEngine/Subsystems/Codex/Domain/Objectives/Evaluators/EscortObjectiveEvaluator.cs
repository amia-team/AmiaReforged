using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Evaluates escort objectives. Tracks an NPC's status (alive/dead, proximity)
/// via signals and completes when the NPC reaches a destination waypoint,
/// or fails if the NPC dies.
/// </summary>
public sealed class EscortObjectiveEvaluator : IObjectiveEvaluator
{
    public string TypeTag => "escort";

    /// <summary>Config key: the NPC tag being escorted (defaults to TargetTag).</summary>
    public const string NpcTagKey = "npc_tag";

    /// <summary>Config key: destination waypoint tag that completes the escort.</summary>
    public const string DestinationKey = "destination";

    /// <summary>Config key: when true, NPC death fails the objective (default: true).</summary>
    public const string FailOnDeathKey = "fail_on_death";

    // Custom state keys
    private const string WaypointsReachedKey = "waypoints_reached";
    private const string NpcAliveKey = "npc_alive";

    public void Initialize(ObjectiveDefinition definition, ObjectiveState state)
    {
        state.SetCustom(WaypointsReachedKey, new HashSet<string>());
        state.SetCustom(NpcAliveKey, true);
    }

    public EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        if (state.IsTerminal)
            return EvaluationResult.NoOp();

        string npcTag = definition.GetConfig<string>(NpcTagKey) ?? definition.TargetTag ?? string.Empty;
        string? destination = definition.GetConfig<string>(DestinationKey);
        bool failOnDeath = definition.Config.ContainsKey(FailOnDeathKey)
            ? definition.GetConfig<bool>(FailOnDeathKey)
            : true; // Default: NPC death = failure

        // Handle NPC death
        if (signal.Matches(SignalType.CreatureKilled, npcTag))
        {
            if (failOnDeath)
            {
                state.IsFailed = true;
                state.IsActive = false;
                state.SetCustom(NpcAliveKey, false);
                return EvaluationResult.Failed($"Escort target '{npcTag}' has died");
            }
        }

        // Handle NPC status change (custom status signals)
        if (signal.Matches(SignalType.NpcStatusChanged, npcTag))
        {
            string? status = signal.GetPayload<string>("status");
            if (status == "dead" && failOnDeath)
            {
                state.IsFailed = true;
                state.IsActive = false;
                state.SetCustom(NpcAliveKey, false);
                return EvaluationResult.Failed($"Escort target '{npcTag}' has died");
            }
        }

        // Handle waypoint reached
        if (signal.SignalType == SignalType.WaypointReached)
        {
            // Check if this waypoint signal is for our NPC
            string? waypointNpc = signal.GetPayload<string>("npc_tag");
            if (!string.Equals(waypointNpc, npcTag, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(signal.TargetTag, npcTag, StringComparison.OrdinalIgnoreCase))
            {
                return EvaluationResult.NoOp();
            }

            string waypointTag = signal.GetPayload<string>("waypoint") ?? signal.TargetTag;

            HashSet<string> waypoints = state.GetCustom<HashSet<string>>(WaypointsReachedKey) ?? [];
            waypoints.Add(waypointTag);
            state.SetCustom(WaypointsReachedKey, waypoints);
            state.CurrentCount = waypoints.Count;

            // Check if destination reached
            if (destination != null &&
                string.Equals(waypointTag, destination, StringComparison.OrdinalIgnoreCase))
            {
                state.IsCompleted = true;
                state.IsActive = false;
                return EvaluationResult.Completed($"Escort complete — {npcTag} reached {destination}");
            }

            return EvaluationResult.Progressed(
                $"Waypoint '{waypointTag}' reached ({waypoints.Count} waypoints)");
        }

        return EvaluationResult.NoOp();
    }
}
