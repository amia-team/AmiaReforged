using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Arcane.Cantrips.ViciousMockery;

[ServiceBinding(typeof(ISpell))]
public class ViciousMockery : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public string ImpactScript => "am_c_vicsmock";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;

        bool hasFocus = false;
        bool hasGreaterFocus = false;
        bool hasEpicFocus = false;
        int chaMod = 0;
        if (caster is NwCreature casterCreature)
        {
            hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusEnchantment);
            hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusEnchantment);
            hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusEnchantment);

            chaMod = casterCreature.GetAbilityModifier(Ability.Charisma);
        }

        NwGameObject? target = eventData.TargetObject;
        if (target == null) return;

        // Only works on creatures
        if (target is not NwCreature targetCreature) return;

        if (target == caster)
        {
            NWScript.FloatingTextStringOnCreature(sStringToDisplay: "You can't mock yourself!", targetCreature,
                NWScript.FALSE);
            return;
        }

        int focusDice = hasFocus ? 1 : hasGreaterFocus ? 2 : hasEpicFocus ? 3 : 0;
        int damage = NWScript.d4(caster.CasterLevel / 3 + focusDice);

        const int concentrationPenalty = 10;
        
        SpellUtils.SignalSpell(caster, target, eventData.Spell);
        
        if (ResistedSpell)
        {
            Effect damageEffect = Effect.Damage(damage, DamageType.Sonic);
            Effect skillPenalty =
                Effect.SkillDecrease(NwSkill.FromSkillType(Skill.Concentration)!, concentrationPenalty);
            skillPenalty.Tag = "VICIOUS_MOCKERY";
            target.ApplyEffect(EffectDuration.Instant, damageEffect);


            Effect? existingSkillPenalty =
                targetCreature.ActiveEffects.SingleOrDefault(e => e.Tag == "VICIOUS_MOCKERY");
            if (existingSkillPenalty != null) targetCreature.RemoveEffect(existingSkillPenalty);

            if (hasEpicFocus)
            {
                targetCreature.ApplyEffect(EffectDuration.Temporary, skillPenalty, TimeSpan.FromSeconds(18));
            }
            else
            {
                SavingThrowResult result = targetCreature.RollSavingThrow(SavingThrow.Will,
                    10 + caster.CasterLevel + chaMod, SavingThrowType.Spell);

                if (result == SavingThrowResult.Failure)
                    targetCreature.ApplyEffect(EffectDuration.Temporary, skillPenalty, TimeSpan.FromSeconds(18));
            }
        }
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }
}