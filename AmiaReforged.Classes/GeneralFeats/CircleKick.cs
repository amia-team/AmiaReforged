using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.GeneralFeats;

[ServiceBinding(typeof(CircleKick))]

public class CircleKick
{
    private const int CircleKickId = 1368;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CircleKick()
    {
        NwModule.Instance.OnUseFeat += ToggleCircleKick;
        Log.Info(message: "Circle Kick initialized.");
    }

    private static void ToggleCircleKick(OnUseFeat eventData)
    {
        if (eventData.Feat.Id != CircleKickId) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        NwCreature kicker = eventData.Creature;

        Effect? circleKickEffect = kicker.ActiveEffects.FirstOrDefault(e => e.EffectType == EffectType.BonusFeat
                                                                         && e.IntParams[0] == (int)Feat.CircleKick);

        if (circleKickEffect != null)
        {
            kicker.RemoveEffect(circleKickEffect);
            player.FloatingTextString("*Circle Kick deactivated*", false, false);
            return;
        }

        circleKickEffect = Effect.BonusFeat(Feat.CircleKick!);
        circleKickEffect.SubType = EffectSubType.Unyielding;

        kicker.ApplyEffect(EffectDuration.Permanent, circleKickEffect);

        player.FloatingTextString("*Circle Kick activated*", false, false);
    }
}
