using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog.Fluent;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(FloatingLeafLeapService))]
public class FloatingLeafLeapService
{
    public FloatingLeafLeapService()
    {
        NwModule.Instance.OnUseFeat += InterceptEmptyBodyToLeap;
    }

    private void InterceptEmptyBodyToLeap(OnUseFeat eventData)
    {
        if (eventData.Feat.Id != MonkFeat.EmptyBodyNew
            || MonkUtils.GetMonkPath(eventData.Creature) != PathType.FloatingLeaf
            || !MonkUtils.GetKiFocus(eventData.Creature).HasValue) return;

        if (FloatingLeaf.TryWeightlessLeap(eventData.Creature))
            eventData.PreventFeatUse = true;
    }
}
