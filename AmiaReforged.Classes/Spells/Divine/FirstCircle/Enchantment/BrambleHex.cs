using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.FirstCircle.Enchantment;

/// <summary>
/// Level: Druid 1
/// Area of effect: Single
/// Duration: 1 Round/level
/// Valid Metamagic: Still, Extend, Silent
/// Save: Will Negates
/// Spell Resistance: Yes
/// This witch-hex will attempt to lay a curse of clumsiness upon the victim.
/// The victim must make a will save. If the save is failed, the victim will be knocked down for 1 round,
/// and be cursed with clumsiness, taking 1d6+1 points of dexterity damage for 1 round per caster level.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class BrambleHex : ISpell
{
    public string ImpactScript => "bramble_hex";
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

        TimeSpan knockdownDuration = NwTimeSpan.FromRounds(1);
        TimeSpan curseDuration = NwTimeSpan.FromRounds(caster.CasterLevel);
        if (eventData.MetaMagicFeat == MetaMagic.Extend)
        {
            knockdownDuration *= 2;
            curseDuration *= 2;
        }

        Effect knockdownEffect = Effect.LinkEffects(Effect.Knockdown(), Effect.VisualEffect(VfxType.DurEntangle));
        knockdownEffect.SubType = EffectSubType.Supernatural;

        int dexDamage = Random.Shared.Roll(6) + 1;
        Effect curseEffect = Effect.LinkEffects
        (
            Effect.Curse(dexMod: dexDamage, chaMod: 0, conMod: 0, intMod: 0, wisMod: 0, strMod: 0),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );
        curseEffect.SubType = EffectSubType.Supernatural;

        creature.ApplyEffect(EffectDuration.Temporary, knockdownEffect, knockdownDuration);
        creature.ApplyEffect(EffectDuration.Temporary, curseEffect, curseDuration);
        creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpCharm));
    }

    public void SetSpellResisted(bool result) { }
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }
}
