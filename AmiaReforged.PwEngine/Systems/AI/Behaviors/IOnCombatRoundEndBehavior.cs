using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnCombatRoundEndBehavior
{
    string ScriptName { get; }
    void OnCombatRoundEnd(CreatureEvents.OnCombatRoundEnd eventData);
}