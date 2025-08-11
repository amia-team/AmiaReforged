using Anvil.API;
using AmiaReforged.Classes.Spells;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.GeneralFeats;

[ServiceBinding(typeof(ISpell))]
public class BlindingSpeed : ISpell
{
    private const string BlindingSpeedCdTag = "blinding_speed_cd";
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public string ImpactScript => "x2_s2_blindspd";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature creature) return;

        Effect? blindingSpeedCd = creature.ActiveEffects.FirstOrDefault(effect => effect.Tag == BlindingSpeedCdTag);

        if (blindingSpeedCd != null)
        {
            if (creature.IsPlayerControlled(out NwPlayer? player))
                SpellUtils.SendRemainingCoolDown(player, eventData.Spell.Name.ToString(), blindingSpeedCd.DurationRemaining);

            return;
        }

        Effect blindingSpeed = Effect.LinkEffects(Effect.Haste(), Effect.VisualEffect(VfxType.DurCessatePositive));
        blindingSpeed.SubType = EffectSubType.Extraordinary;

        creature.ApplyEffect(EffectDuration.Temporary, blindingSpeed, NwTimeSpan.FromTurns(1));
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpDustExplosion));

        blindingSpeedCd = Effect.VisualEffect(VfxType.None);
        blindingSpeedCd.Tag = BlindingSpeedCdTag;
        blindingSpeedCd.SubType = EffectSubType.Extraordinary;

        creature.ApplyEffect(EffectDuration.Temporary, blindingSpeedCd, NwTimeSpan.FromRounds(15));
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}


