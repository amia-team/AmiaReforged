using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.AI.Behaviors.Generic;

[ServiceBinding(typeof(IOnCombatRoundEndBehavior))]
public class GenericOnCombatRoundEnd : IOnCombatRoundEndBehavior
{
    public string ScriptName => "ds_ai2_endround";

    public void OnCombatRoundEnd(CreatureEvents.OnCombatRoundEnd eventData)
    {
        CheckLivingBulwark(eventData);
    }

    private void CheckLivingBulwark(CreatureEvents.OnCombatRoundEnd eventData)
    {
        NwGameObject? currentTarget = eventData.Creature.AttackTarget;
        if (currentTarget is null) return;


        if (currentTarget.GetObjectVariable<LocalVariableInt>(name: "guardCD") == 1)
            eventData.Creature.ClearActionQueue();
    }
}