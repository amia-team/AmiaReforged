using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.RayofFrost;

[DecoratesSpell(typeof(RayOfFrost))]
public class RayOfFrostSpecializationDecorator : SpellDecorator
{
    private const float AoERadius = 3f;

    public RayOfFrostSpecializationDecorator(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override string ImpactScript => Spell.ImpactScript;

    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;
        if (target is not NwCreature creature) return;


        if (caster is not NwCreature casterCreature) return;

        bool isConjurer = casterCreature.GetSpecialization(NwClass.FromClassType(ClassType.Wizard)) ==
                          SpellSchool.Conjuration;

        if (isConjurer)
        {
            Effect coldBoom = Effect.VisualEffect(VfxType.ImpFrostL, false, 2f);

            List<NwCreature> creaturesInFiveMeterRadius = casterCreature.GetNearestCreatures()
                .Where(c => c.IsReactionTypeHostile(casterCreature) && c.Distance(target) <= AoERadius)
                .ToList();

            creature.ApplyEffect(EffectDuration.Instant, coldBoom);

            int diceRoll = NWScript.d3(casterCreature.CasterLevel / 2);

            // This is just a cantrip, so the extra damage shouldn't be anything crazy.
            int damage = diceRoll / 2;
            creaturesInFiveMeterRadius.ForEach(c =>
                c.ApplyEffect(EffectDuration.Instant, Effect.Damage(damage, DamageType.Cold)));
        }

        base.OnSpellImpact(eventData);
    }
}