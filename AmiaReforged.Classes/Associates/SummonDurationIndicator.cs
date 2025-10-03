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

        string? summonName = eventData.Effect.ObjectParams[1]?.Name;

        TimeSpan cdTimeSpan = TimeSpan.FromSeconds(eventData.Effect.DurationRemaining);

        string formatTime = cdTimeSpan.TotalMinutes >= 1 ? $"{cdTimeSpan.Minutes}m {cdTimeSpan.Seconds}s"
            : $"{cdTimeSpan.Seconds}s";

        string message = summonName != null ? $"{summonName} duration: {formatTime}"
            : $"Summoned creature duration: {formatTime}";

        player.SendServerMessage(message);
    }
}
