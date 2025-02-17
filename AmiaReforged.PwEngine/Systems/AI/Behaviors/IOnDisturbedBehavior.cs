using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnDisturbedBehavior
{
    string ScriptName { get; }
    void OnDisturbed(CreatureEvents.OnDisturbed eventData);
}