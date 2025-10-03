using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates;

[ServiceBinding(typeof(SummonDurationIndicator))]
public class SummonDurationIndicator
{
    public SummonDurationIndicator(EventService eventService)
    {
        eventService.SubscribeAll<OnEffectApply, OnEffectApply.Factory>(IndicateSummonDuration, EventCallbackType.After);
    }

    private void IndicateSummonDuration(OnEffectApply eventData)
    {
        if (eventData.Effect.EffectType != EffectType.SummonCreature) return;
        if (!eventData.Object.IsPlayerControlled(out NwPlayer? player)) return;

        TimeSpan durationTimeSpan = TimeSpan.FromSeconds(eventData.Effect.DurationRemaining);

        string formatTime = durationTimeSpan.TotalMinutes >= 1 ? $"{durationTimeSpan.Minutes}m {durationTimeSpan.Seconds}s"
            : $"{durationTimeSpan.Seconds}s";

        player.SendServerMessage($"Summoned creature duration: {formatTime}");
    }
}
