using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates;

[ServiceBinding(typeof(EpicAssociateSanctuaryRemover))]
public class EpicAssociateSanctuaryRemover
{
    public EpicAssociateSanctuaryRemover()
    {
        NwModule.Instance.OnAssociateAdd += OnEpicAssociateRemoveSanctuary;
    }

    private void OnEpicAssociateRemoveSanctuary(OnAssociateAdd eventData)
    {
        if (eventData.Associate.Level < 21) return;

        Effect? sanctuary =
            eventData.Owner.ActiveEffects.FirstOrDefault(e => e.EffectType == EffectType.Sanctuary);

        if (sanctuary == null) return;

        eventData.Owner.RemoveEffect(sanctuary);

        if (eventData.Owner.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage
                ($"Summoning epic level associate {eventData.Associate.Name} has removed your Sanctuary.");
    }
}
