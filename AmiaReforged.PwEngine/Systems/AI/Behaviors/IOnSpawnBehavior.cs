using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnSpawnBehavior
{
    string ScriptName { get; }
    void OnSpawn(CreatureEvents.OnSpawn eventData);
}