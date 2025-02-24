using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnHeartbeatBehavior
{
    string ScriptName { get; }
    void OnHeartbeat(CreatureEvents.OnHeartbeat eventData);
}