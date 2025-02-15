using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using NWN.Core.NWNX;
using static NWN.Core.NWScript;

namespace AmiaReforged.Classes.Spells.Arcane.SecondCircle.Evocation;

public class DarknessSpell : ISpell, IAreaOfEffect
{
    private const double OneRound = 7;
    public const string DarknessBlindTag = "DARKNESS_BLIND";
    private readonly Location _location;
    private readonly NwObject _caster;

    public DarknessSpell(Location location, NwObject caster)
    {
        _caster = caster;
        _location = location;
    }

    public DarknessSpell()
    {
        
    }

    public void Trigger()
    {
        IntPtr darkness = EffectAreaOfEffect(AOE_PER_DARKNESS);

        float duration = RoundsToSeconds(GetCasterLevel(_caster));

        ApplyEffectAtLocation(DURATION_TYPE_TEMPORARY, darkness, _location, duration);
    }

    public void TriggerOnEnter(NwCreature? enteringObject)
    {
        if(enteringObject.IsDMAvatar) return;
        if(enteringObject.ActiveEffects.Any(e => e.EffectType is EffectType.Ultravision or EffectType.TrueSeeing)) return;
        
        
        Effect blind = DarknessBlind();

        enteringObject.ApplyEffect(EffectDuration.Temporary, blind, TimeSpan.FromSeconds(OneRound));
    }

    private static Effect DarknessBlind()
    {
        Effect blind = Effect.Blindness();
        blind.DurationType = EffectDuration.Temporary;
        blind.Tag = DarknessBlindTag;
        blind.IgnoreImmunity = true;
        blind.SubType = EffectSubType.Supernatural;
        return blind;
    }

    public void TriggerHeartbeat(NwCreature lingeringObject)
    {
        if(lingeringObject.ActiveEffects.Any(e => e.EffectType is EffectType.Ultravision or EffectType.TrueSeeing)) return;

        Effect blind = DarknessBlind();
        
        lingeringObject.ApplyEffect(EffectDuration.Temporary, blind, TimeSpan.FromSeconds(OneRound));
    }

    public void TriggerOnExit(NwCreature exitingObject)
    {
        Effect? blind = exitingObject.ActiveEffects.FirstOrDefault(e => e.Tag == DarknessBlindTag);
        
        if(blind == null) return;
        
        exitingObject.RemoveEffect(blind);
    }
}