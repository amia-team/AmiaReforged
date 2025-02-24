using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnBlockedBehavior
{
    string ScriptName { get; }
    void OnBlocked(CreatureEvents.OnBlocked eventData);
}