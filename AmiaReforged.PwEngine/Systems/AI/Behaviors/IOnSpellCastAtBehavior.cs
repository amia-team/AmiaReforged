using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnSpellCastAtBehavior
{
    string ScriptName { get; }
    
    void OnSpellCastAt(CreatureEvents.OnSpellCastAt eventData);
}