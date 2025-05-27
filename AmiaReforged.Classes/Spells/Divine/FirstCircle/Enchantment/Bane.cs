using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Divine.FirstCircle.Enchantment;

/// <summary>
/// Bane fills the caster's enemies with fear and doubt. They suffer a -1 penalty on their attack rolls
/// and a -1 penalty on saving throws against fear. Epic Spell Focus makes this saveless.
/// </summary>
[ServiceBinding(typeof(ISpell))]
public class FireBolt : ISpell
{
    public bool CheckedSpellResistance { get; set; }
    public bool ResistedSpell { get; set; }

    public string ImpactScript => "X0_S0_Bane";

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        NwGameObject? caster = eventData.Caster;
        if (caster == null) return;
        if (caster is not NwCreature casterCreature) return;

        Location? location = eventData.TargetLocation;
        if (location == null) return;

        NwClass? spellClass = eventData.SpellCastClass;
        if (spellClass == null) return;

        int casterLevel = casterCreature.GetClassInfo(spellClass)!.Level;
        int spellDc = SpellUtils.GetSpellDc(eventData);
        TimeSpan spellDuration = NwTimeSpan.FromRounds(casterLevel);

        bool hasEpicFocus = casterCreature.Feats.Any(f => f.FeatType == Feat.EpicSpellFocusEnchantment);

        DoBane();
        return;


        void DoBane()
        {
            location.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfLosEvil30));

            foreach (NwGameObject nwObject in location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
            {
                if (nwObject is not NwCreature targetCreature ||
                    !casterCreature.IsReactionTypeHostile(targetCreature)) continue;

                SpellUtils.SignalSpell(caster, nwObject, eventData.Spell);

                if (ResistedSpell) continue;

                if (hasEpicFocus)
                {
                    ApplyEffect(targetCreature, spellDuration);
                    continue;
                }

                SavingThrowResult savingThrowResult =
                    targetCreature.RollSavingThrow(SavingThrow.Will, spellDc, SavingThrowType.MindSpells,
                        casterCreature);

                if (savingThrowResult is SavingThrowResult.Success)
                {
                    targetCreature.ApplyEffect(EffectDuration.Instant,
                        Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
                    continue;
                }

                ApplyEffect(targetCreature, spellDuration);
            }
        }
    }

    public void SetSpellResisted(bool result)
    {
        ResistedSpell = result;
    }

    private static void ApplyEffect(NwCreature targetCreature, TimeSpan spellDuration)
    {
        Effect baneEffect = Effect.LinkEffects(Effect.SavingThrowDecrease(SavingThrow.Will, 1, SavingThrowType.Fear),
            Effect.AttackDecrease(1), Effect.VisualEffect(VfxType.DurCessateNegative));
        Effect baneVfx = Effect.VisualEffect(VfxType.ImpHeadEvil);

        targetCreature.ApplyEffect(EffectDuration.Instant, baneVfx);
        targetCreature.ApplyEffect(EffectDuration.Temporary, baneEffect, spellDuration);
    }
}