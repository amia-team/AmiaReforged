using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors;

public interface IOnCombatRoundEndBehavior
{
    string ScriptName { get; }
    void OnCombatRoundEnd(CreatureEvents.OnCombatRoundEnd eventData);
}