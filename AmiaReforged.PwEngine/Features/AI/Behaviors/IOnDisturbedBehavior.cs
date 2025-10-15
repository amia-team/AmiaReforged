using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors;

public interface IOnDisturbedBehavior
{
    string ScriptName { get; }
    void OnDisturbed(CreatureEvents.OnDisturbed eventData);
}
