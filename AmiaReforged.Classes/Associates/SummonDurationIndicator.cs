using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates;

[ServiceBinding(typeof(SummonDurationIndicator))]
public class SummonDurationIndicator
{
    public SummonDurationIndicator()
    {
        NwModule.Instance.OnEffectApply += IndicateSummonDuration;
    }

    private void IndicateSummonDuration(OnEffectApply eventData)
    {
        if (eventData.Effect.EffectType != EffectType.SummonCreature) return;
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player)) return;

        TimeSpan summonTimeSpan = TimeSpan.FromSeconds(eventData.Effect.TotalDuration);

        string formatTime =
            summonTimeSpan.TotalHours >= 1 ? $"{summonTimeSpan.Hours}h {summonTimeSpan.Minutes}m {summonTimeSpan.Seconds}s" :
            summonTimeSpan.TotalMinutes >= 1 ? $"{summonTimeSpan.Minutes}m {summonTimeSpan.Seconds}s"
            : $"{summonTimeSpan.Seconds}s";

        player.SendServerMessage($"Summon duration: {formatTime}");
    }
}
