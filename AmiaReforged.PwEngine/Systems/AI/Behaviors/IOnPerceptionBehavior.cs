using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnPerceptionBehavior
{
    string ScriptName { get; }
    void OnPerception(CreatureEvents.OnPerception eventData);
}