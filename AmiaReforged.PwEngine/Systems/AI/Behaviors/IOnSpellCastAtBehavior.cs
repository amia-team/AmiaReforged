using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors;

public interface IOnSpellCastAtBehavior
{
    string ScriptName { get; }

    void OnSpellCastAt(CreatureEvents.OnSpellCastAt eventData);
}