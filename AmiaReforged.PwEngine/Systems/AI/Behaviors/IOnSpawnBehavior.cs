using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors;

public interface IOnSpawnBehavior
{
    string ScriptName { get; }
    void OnSpawn(CreatureEvents.OnSpawn eventData);
}