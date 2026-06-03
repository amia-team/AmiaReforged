using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.EffectUtils.Polymorph;

[ServiceBinding(typeof(GreaterWildshapeBonusRemover))]
public class GreaterWildshapeBonusRemover
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public GreaterWildshapeBonusRemover()
    {
        NwModule.Instance.OnPolymorphRemove += RemoveGreaterWildshapeBonus;
    }

    private void RemoveGreaterWildshapeBonus(OnPolymorphRemove eventData)
    {
        if (PolymorphUtils.RemoveGreaterWildshapeBonus(eventData.Creature))
            Log.Info($"Greater Wildshape bonus removed from {eventData.Creature.Name}.");
    }
}
