using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors;

public interface IOnRestedBehavior
{
    string ScriptName { get; }
    void OnRested(CreatureEvents.OnRested eventData);
}
