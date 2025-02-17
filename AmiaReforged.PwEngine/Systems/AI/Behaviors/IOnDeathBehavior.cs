using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnDeathBehavior
{
    string ScriptName { get; }
    void OnDeath(CreatureEvents.OnDeath eventData);
}