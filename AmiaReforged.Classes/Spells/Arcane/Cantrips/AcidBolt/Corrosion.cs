using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.AcidBolt;

[DecoratesSpell(typeof(AcidBolt))]
public class Corrosion : SpellDecorator
{
    public Corrosion(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (caster is not NwCreature casterCreature) return;

        bool hasConjurationSpecialization = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) ==
                                            SpellSchool.Conjuration;

        if (hasConjurationSpecialization)
        {
            ApplyImpactVfx(target);

            Effect corrosion = Effect.ACDecrease(2, ACBonus.ArmourEnchantment);
            corrosion.Tag = "AM_AcidBolt_Corrosion";

            ApplyCorrosion(target, corrosion);
        }

        Spell.OnSpellImpact(eventData);
    }

    private static void ApplyImpactVfx(NwGameObject creature)
    {
        Effect acidBoom = Effect.VisualEffect(VfxType.ImpDustExplosion);
        creature.ApplyEffect(EffectDuration.Instant, acidBoom);
    }

    private void ApplyCorrosion(NwGameObject creature, Effect corrosion)
    {
        if (!ResistedSpell)
        {
            RemoveExistingEffect(creature);
            creature.ApplyEffect(EffectDuration.Instant, corrosion);
        }
    }

    private static void RemoveExistingEffect(NwGameObject creature)
    {
        Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "AM_AcidBolt_Corrosion");
        if (existing != null) creature.RemoveEffect(existing);
    }
}