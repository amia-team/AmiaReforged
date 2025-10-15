using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors;

public interface IOnSpellCastAtBehavior
{
    string ScriptName { get; }

    void OnSpellCastAt(CreatureEvents.OnSpellCastAt eventData);
}
