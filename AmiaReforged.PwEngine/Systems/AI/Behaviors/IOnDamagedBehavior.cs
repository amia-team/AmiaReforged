using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors;

public interface IOnDamagedBehavior
{
    string ScriptName { get; }
    void OnDamaged(CreatureEvents.OnDamaged eventData);
}