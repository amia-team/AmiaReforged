using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors;

public interface IOnDisturbedBehavior
{
    string ScriptName { get; }
    void OnDisturbed(CreatureEvents.OnDisturbed eventData);
}