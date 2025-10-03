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

        Effect[] sanctuaryEffects = eventData.Owner.ActiveEffects
            .Where(e => e.EffectType is EffectType.Sanctuary or EffectType.Ethereal)
            .ToArray();

        if (sanctuaryEffects.Length == 0) return;

        foreach (Effect effect in sanctuaryEffects)
            eventData.Owner.RemoveEffect(effect);

        if (eventData.Owner.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage
                ($"Summoning epic level associate {eventData.Associate.Name} has removed your Sanctuary.");
    }
}
