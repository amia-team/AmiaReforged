using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells.Arcane.FirstCircle.Necromancy;

[DecoratesSpell(typeof(NegativeEnergyRay))]
public class NegativeEnergyRayFocusDecorator : SpellDecorator
{
    private const double FiveRounds = 30;
    private const int VulnerabilityPerFocus = 5;
    private const string EffectTag = "NegativeEnergyRayFocusDecorator";

    public NegativeEnergyRayFocusDecorator(ISpell spell) : base(spell)
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

        bool hasBasicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        bool hasAnyFocus = hasBasicFocus || hasGreaterFocus || hasEpicFocus;

        if (hasAnyFocus && !ResistedSpell)
        {
            int vulnerabilityPercentage = CalculateVulnerability(hasBasicFocus, hasGreaterFocus, hasEpicFocus);

            Effect vulnerability = Effect.DamageImmunityDecrease(DamageType.Negative, vulnerabilityPercentage);
            Effect vfx = Effect.VisualEffect(VfxType.DurAuraRedDark);
            vulnerability = Effect.LinkEffects(vulnerability, vfx);
            vulnerability.Tag = EffectTag;

            RemoveExistingEffect(creature);
            creature.ApplyEffect(EffectDuration.Temporary, vulnerability, TimeSpan.FromSeconds(FiveRounds));
        }

        Spell.OnSpellImpact(eventData);
    }

    private static int CalculateVulnerability(bool hasBasicFocus, bool hasGreaterFocus, bool hasEpicFocus)
    {
        int focusCount = 0;
        if (hasBasicFocus) focusCount++;
        if (hasGreaterFocus) focusCount++;
        if (hasEpicFocus) focusCount++;

        return focusCount * VulnerabilityPerFocus;
    }

    private static void RemoveExistingEffect(NwCreature creature)
    {
        Effect? existing = creature.ActiveEffects.FirstOrDefault(e => e.Tag == EffectTag);
        if (existing != null) creature.RemoveEffect(existing);
    }
}

