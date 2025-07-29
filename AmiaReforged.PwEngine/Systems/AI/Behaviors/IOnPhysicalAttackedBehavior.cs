using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors;

public interface IOnPhysicalAttackedBehavior
{
    string ScriptName { get; }
    void OnPhysicalAttacked(CreatureEvents.OnPhysicalAttacked eventData);
}