using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Divine.Cantrips.CureMinorWounds;

[ServiceBinding(typeof(CureMinorWounds))]
public class CureMinorWounds : ISpell
{
    public ResistSpellResult Result { get; set; }
    public string ImpactScript => "NW_S0_CurMinW";

    public void DoSpellResist(NwCreature creature, NwCreature caster)
    {
        Result = creature.CheckResistSpell(caster);
    }

    public void OnSpellImpact(SpellEvents.OnSpellCast eventData)
    {
        if (eventData.Caster == null) return;
        if (eventData.Caster is not NwCreature casterCreature) return;

        if (eventData.TargetObject == null) return;

        bool skipTouchAttack = NWScript.GetRacialType(eventData.TargetObject) != NWScript.RACIAL_TYPE_UNDEAD;
        Task<TouchAttackResult> result = casterCreature.TouchAttackRanged(eventData.TargetObject, !skipTouchAttack);

        if (result.Result != TouchAttackResult.Hit || !skipTouchAttack) return;


        int healAmount = CalculateHealAmount(casterCreature);

        if (Result != ResistSpellResult.Failed || !skipTouchAttack) return;

        ApplyEffect(eventData, healAmount);
    }

    public void SetSpellResistResult(ResistSpellResult result)
    {
        Result = result;
    }

    private int CalculateHealAmount(NwCreature casterCreature)
    {
        bool hasFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.SpellFocusNecromancy);
        bool hasGreaterFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.GreaterSpellFocusNecromancy);
        bool hasEpicFocus = casterCreature.Feats.Any(f => f.Id == (ushort)Feat.EpicSpellFocusNecromancy);

        int bonusHeal = hasFocus ? 1 : hasGreaterFocus ? 2 : hasEpicFocus ? 3 : 0;

        int numDie = casterCreature.CasterLevel / 2 + bonusHeal;

        return NWScript.d2(numDie);
    }

    private void ApplyEffect(SpellEvents.OnSpellCast eventData, int healAmount)
    {
        Effect effectBasedOnType = NWScript.GetRacialType(eventData.TargetObject) == NWScript.RACIAL_TYPE_UNDEAD
            ? Effect.Damage(healAmount, DamageType.Negative)
            : Effect.Heal(healAmount);

        Effect vfx = NWScript.GetRacialType(eventData.TargetObject) == NWScript.RACIAL_TYPE_UNDEAD
            ? Effect.VisualEffect(VfxType.ComHitDivine)
            : Effect.VisualEffect(VfxType.ImpHealingS);

        eventData.TargetObject!.ApplyEffect(EffectDuration.Instant, effectBasedOnType);
        eventData.TargetObject!.ApplyEffect(EffectDuration.Instant, vfx);
    }
}