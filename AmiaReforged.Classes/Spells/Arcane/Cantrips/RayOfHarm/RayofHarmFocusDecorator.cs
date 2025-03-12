using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.RayOfHarm;

[ServiceBinding(typeof(RayofHarmFocusDecorator))]
public class RayofHarmFocusDecorator : SpellDecorator
{
    public RayofHarmFocusDecorator(ISpell spell) : base(spell)
    {
        Spell = spell;
    }

    public override string ImpactScript => Spell.ImpactScript;

    // Each spell focus reduces the target's physical damage by 1
    public override void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        if (target is not NwCreature creature) return;
        if (caster is not NwCreature casterCreature) return;

        bool basicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool greaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool epicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        bool isNecromancyFocused = basicFocus || greaterFocus || epicFocus;

        if (isNecromancyFocused && !ResistedSpell)
        {
            int reducedDamageAmount = epicFocus ? 6 : greaterFocus ? 4 : basicFocus ? 2 : 0;

            Effect decreasedDamage = Effect.DamageDecrease(reducedDamageAmount, DamageType.BaseWeapon);

            Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == "RayofHarmFocusDecorator");

            if (existing != null) creature.RemoveEffect(existing);
            target.ApplyEffect(EffectDuration.Temporary, decreasedDamage, TimeSpan.FromSeconds(12));
        }

        Spell.OnSpellImpact(eventData);
    }
}