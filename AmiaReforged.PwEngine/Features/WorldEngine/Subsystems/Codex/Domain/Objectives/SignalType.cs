namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// Well-known signal types that NWN event adapters translate game events into.
/// These constants decouple the quest domain from NWN-specific event types.
/// </summary>
public static class SignalType
{
    public const string CreatureKilled = "creature_killed";
    public const string ItemAcquired = "item_acquired";
    public const string ItemLost = "item_lost";
    public const string AreaEntered = "area_entered";
    public const string AreaExited = "area_exited";
    public const string DialogChoice = "dialog_choice";
    public const string ClueFound = "clue_found";
    public const string NpcStatusChanged = "npc_status_changed";
    public const string WaypointReached = "waypoint_reached";
    public const string Custom = "custom";
}
