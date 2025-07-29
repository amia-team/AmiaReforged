using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors;

public interface IOnPerceptionBehavior
{
    string ScriptName { get; }
    void OnPerception(CreatureEvents.OnPerception eventData);
}