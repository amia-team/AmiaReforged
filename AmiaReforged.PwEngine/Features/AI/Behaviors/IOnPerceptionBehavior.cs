using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors;

public interface IOnPerceptionBehavior
{
    string ScriptName { get; }
    void OnPerception(CreatureEvents.OnPerception eventData);
}
