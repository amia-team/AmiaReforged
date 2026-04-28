using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Warlock.PactSummon;

[ServiceBinding(typeof(PactSummonStatApplier))]
public class PactSummonStatApplier
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public PactSummonStatApplier()
    {
        NwModule.Instance.OnAssociateAdd += ApplyPactSummonStats;

        Log.Info("Pact Summon Stat Handler initialized.");
    }

    private static void ApplyPactSummonStats(OnAssociateAdd eventData)
    {
        if (eventData.AssociateType != AssociateType.Summoned
            || !eventData.Associate.IsPactSummon()) return;

        NwCreature pactSummon = eventData.Associate;
        int invocationCl = eventData.Owner.GetInvocationCasterLevel();
        int summonTier = PactSummonTable.GetSummonTier(invocationCl);

        PactSummonBaseData baseData = PactSummonTable.GetBaseData(pactSummon.ResRef);
        PactSummonTierData tierData = PactSummonTable.GetTierData(pactSummon.ResRef, summonTier);

        ApplyEffects(pactSummon, baseData.ImmunityTypes, baseData.SharedEffect, tierData.TierEffect, tierData.DamageBonus);

        CreaturePlugin.SetLevelByPosition(pactSummon, 0, invocationCl);

        pactSummon.MovementRate = baseData.MovementRate;
        pactSummon.Size = baseData.Size;
        pactSummon.MaxHP = tierData.HitPoints;
        pactSummon.HP = tierData.HitPoints;
        pactSummon.BaseAC = tierData.ArmorBonus;
        pactSummon.BaseAttackBonus = tierData.BaseAttackBonus;
        pactSummon.BaseAttackCount = tierData.BaseAttackCount;
        pactSummon.SetsRawAbilityScore(Ability.Strength, tierData.Strength);
        pactSummon.SetBaseSavingThrow(SavingThrow.Fortitude, tierData.BaseSavingThrow);
        pactSummon.SetBaseSavingThrow(SavingThrow.Reflex, tierData.BaseSavingThrow);
        pactSummon.SetBaseSavingThrow(SavingThrow.Will, tierData.BaseSavingThrow);

        foreach (Skill skill in baseData.Skills)
            pactSummon.SetSkillRank(skill!, tierData.SkillRank);
    }

    private static void ApplyEffects(
        NwCreature pactSummon,
        ImmunityType[]? immunities,
        Effect? sharedEffect,
        Effect? tierEffect,
        DamageBonus? damageBonus)
    {
        Effect effect = Effect.VisualEffect(VfxType.None);

        if (immunities != null)
        {
            foreach (ImmunityType immunity in immunities)
                effect = Effect.LinkEffects(effect, Effect.Immunity(immunity));
        }

        if (sharedEffect != null)
            effect = Effect.LinkEffects(effect, sharedEffect);
        if (tierEffect != null)
            effect = Effect.LinkEffects(effect, tierEffect);
        if (damageBonus != null)
            effect = Effect.LinkEffects(effect, Effect.DamageIncrease(damageBonus.Value, DamageType.BaseWeapon));

        effect.SubType = EffectSubType.Unyielding;

        pactSummon.ApplyEffect(EffectDuration.Permanent, effect);
    }
}
