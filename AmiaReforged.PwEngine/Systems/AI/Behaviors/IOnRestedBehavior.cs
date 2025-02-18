using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnRestedBehavior
{
    string ScriptName { get; }
    void OnRested(CreatureEvents.OnRested eventData);
}