using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnPhysicalAttackedBehavior
{
    string ScriptName { get; }
    void OnPhysicalAttacked(CreatureEvents.OnPhysicalAttacked eventData);
}