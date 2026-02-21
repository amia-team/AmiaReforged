using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.SecondCircle.Necromancy;

/// <summary>
/// Level: Cleric 2
/// Area of effect: Single
/// Duration: 1 Round/level
/// Valid Metamagic: Still, Extend, Silent
/// Save: Will Negates
/// Spell Resistance: Yes
/// The cleric utters a curse upon enemies of their faith. The curse causes weariness and exhaustion,
/// inflicting two damage to strength and constitution and 30% movement speed decrease.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class ProfaneShackle : ISpell
{
    public string ImpactScript => "profane_shackle";
    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.TargetObject is not NwCreature creature
            || eventData.Caster is not NwCreature caster
            || caster.IsReactionTypeFriendly(creature)) return;

        CreatureEvents.OnSpellCastAt.Signal(caster, creature, eventData.Spell);
        if (caster.SpellResistanceCheck(creature, eventData.Spell, creature.CasterLevel))
            return;

        if (creature.RollSavingThrow(SavingThrow.Will, eventData.SaveDC, SavingThrowType.Spell, caster)
            == SavingThrowResult.Success)
        {
            creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
            return;
        }

        TimeSpan duration = NwTimeSpan.FromRounds(caster.CasterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend) duration *= 2;

        Effect curseEffect = Effect.LinkEffects
        (
            Effect.Curse(strMod: 2, dexMod: 0, conMod: 2, intMod: 0, wisMod: 0, chaMod: 0),
            Effect.MovementSpeedDecrease(30),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );
        curseEffect.SubType = EffectSubType.Supernatural;

        creature.ApplyEffect(EffectDuration.Temporary, curseEffect, duration);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReduceAbilityScore));
    }

    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
    public void SetSpellResisted(bool result) { }
}
