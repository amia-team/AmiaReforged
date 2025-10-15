using AmiaReforged.PwEngine.Features.Chat.Commands;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Commands;

[ServiceBinding(typeof(IChatCommand))]
public class ConfirmEyeGlow : IChatCommand
{
    private const string MonkEyeGlowTag = "monk_eye_glow";
    public string Command => "./confirmeyeglow";
    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        NwCreature? monk = caller.ControlledCreature;
        if (monk == null) return Task.CompletedTask;

        Effect? existingEyeGlow = monk.ActiveEffects.FirstOrDefault(e => e.Tag == MonkEyeGlowTag);

        if (existingEyeGlow == null)
        {
            caller.
                FloatingTextString("Eye glow visual effect not found. Try using the Perfect Self feat to " +
                                   "reapply the eye glow you want as your permanent choice.", false);

            return Task.CompletedTask;
        }

        VfxType eyeGlowVfx = (VfxType)existingEyeGlow.IntParams[0];
        float scale = monk.VisualTransform.Scale;

        monk.RemoveEffect(existingEyeGlow);

        Effect eyeGlowEffect = Effect.VisualEffect(eyeGlowVfx, fScale: scale);
        eyeGlowEffect.SubType = EffectSubType.Unyielding;
        eyeGlowEffect.Tag = MonkEyeGlowTag;

        monk.ApplyEffect(EffectDuration.Permanent, eyeGlowEffect);

        caller.FloatingTextString("Monk eye glow added permanently!", false);

        return Task.CompletedTask;
    }
}
