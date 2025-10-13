using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public sealed class CrashingMeteor : IAugmentation
{
    private const string MeteorKiShoutTag = nameof(PathType.CrashingMeteor) + nameof(TechniqueType.KiShout);
    public PathType PathType => PathType.CrashingMeteor;
    public void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureAttack attackData)
    {
        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        switch (technique)
        {
            case TechniqueType.StunningStrike:
                AugmentStunningStrike(monk, attackData, meteor);
                break;
            case TechniqueType.AxiomaticStrike:
                AugmentAxiomaticStrike(attackData, meteor);
                break;
            case TechniqueType.EagleStrike:
                EagleStrike.DoEagleStrike(monk, attackData);
                break;
        }
    }
    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        switch (technique)
        {
            case TechniqueType.WholenessOfBody:
                AugmentWholenessOfBody(monk, meteor);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(monk, meteor);
                break;
            case TechniqueType.EmptyBody:
                EmptyBody.DoEmptyBody(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.QuiveringPalm:
                QuiveringPalm.DoQuiveringPalm(monk, castData);
                break;
        }
    }

    private struct CrashingMeteorData
    {
        public int Dc;
        public int DiceAmount;
        public short BonusDamage;
        public Effect AoeVfx;
        public VfxType DamageVfx;
        public DamageType DamageType;
        public SavingThrowType SaveType;
        public int DamageVulnerability;
    }

    private static CrashingMeteorData GetCrashingMeteorData(NwCreature monk)
    {
        DamageType elementalType = MonkUtils.GetElementalType(monk);

        return new CrashingMeteorData
        {
            Dc = MonkUtils.CalculateMonkDc(monk),
            DiceAmount = MonkUtils.GetKiFocus(monk) switch
            {
                KiFocus.KiFocus1 => 4,
                KiFocus.KiFocus2 => 6,
                KiFocus.KiFocus3 => 8,
                _ => 2
            },
            BonusDamage = MonkUtils.GetKiFocus(monk) switch
            {
                KiFocus.KiFocus1 => 2,
                KiFocus.KiFocus2 => 3,
                KiFocus.KiFocus3 => 4,
                _ => 1
            },
            AoeVfx = MonkUtils.ResizedVfx(elementalType switch
            {
                DamageType.Fire => VfxType.FnfFirestorm,
                DamageType.Cold => VfxType.ImpFrostL,
                DamageType.Electrical => VfxType.FnfElectricExplosion,
                DamageType.Acid => VfxType.ImpAcidS,
                _ => VfxType.FnfFireball
            }, RadiusSize.Large),
            DamageVfx = elementalType switch
            {
                DamageType.Fire => VfxType.ImpFlameS,
                DamageType.Cold => VfxType.ImpFrostS,
                DamageType.Electrical => VfxType.ComHitElectrical,
                DamageType.Acid => VfxType.ImpAcidS,
                _ => VfxType.ImpFlameS
            },
            DamageType = elementalType,
            SaveType = elementalType switch
            {
                DamageType.Fire => SavingThrowType.Fire,
                DamageType.Cold => SavingThrowType.Cold,
                DamageType.Electrical => SavingThrowType.Electricity,
                DamageType.Acid => SavingThrowType.Acid,
                _ => SavingThrowType.Fire
            },
            DamageVulnerability = MonkUtils.GetKiFocus(monk) switch
            {
                KiFocus.KiFocus1 => 20,
                KiFocus.KiFocus2 => 30,
                KiFocus.KiFocus3 => 40,
                _ => 10
            }
        };
    }

    /// <summary>
    ///     Stunning Strike deals 2d6 elemental damage in a large area around the target. The damage isn’t multiplied by
    ///     critical hits and a successful reflex save halves the damage. Each Ki Focus adds 2d6 to a maximum of 8d6 elemental
    ///     damage.
    /// </summary>
    private void AugmentStunningStrike(NwCreature monk, OnCreatureAttack attackData,
        CrashingMeteorData meteor)
    {
        StunningStrike.DoStunningStrike(attackData);

        attackData.Target.ApplyEffect(EffectDuration.Instant, meteor.AoeVfx);

        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true,
                     ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Placeable))
        {
            int damageAmount = Random.Shared.Roll(6, meteor.DiceAmount);
            if (nwObject is not NwCreature creatureInShape)
            {
                _ = ApplyStunningDamage(nwObject, monk, damageAmount, meteor.DamageType, meteor.DamageVfx);
                continue;
            }

            if (monk.IsReactionTypeFriendly(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.Fireball)!);

            bool hasEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
            bool hasImprovedEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

            SavingThrowResult savingThrowResult =
                creatureInShape.RollSavingThrow(SavingThrow.Reflex, meteor.Dc, meteor.SaveType, monk);

            if ((hasEvasion || hasImprovedEvasion) &&  savingThrowResult == SavingThrowResult.Success)
            {
                creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
                continue;
            }

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            _ = ApplyStunningDamage(creatureInShape, monk, damageAmount, meteor.DamageType, meteor.DamageVfx);
        }
    }

    private async Task ApplyStunningDamage(NwGameObject targetObject, NwCreature monk, int damageAmount,
        DamageType damageType, VfxType damageVfx)
    {
        await monk.WaitForObjectContext();
        Effect damageEffect = Effect.LinkEffects(
            Effect.Damage(damageAmount, damageType),
            Effect.VisualEffect(damageVfx));

        targetObject.ApplyEffect(EffectDuration.Instant, damageEffect);
    }


    /// <summary>
    ///     Axiomatic Strike deals +1 bonus elemental damage, with an additional +1 for every Ki Focus,
    ///     to a maximum of +4 elemental damage.
    /// </summary>
    private void AugmentAxiomaticStrike(OnCreatureAttack attackData, CrashingMeteorData meteor)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        DamageData<short> damageData = attackData.DamageData;
        short elementalDamage = damageData.GetDamageByType(meteor.DamageType);

        short bonusDamage = meteor.BonusDamage;
        if (elementalDamage == -1) bonusDamage++;

        elementalDamage += bonusDamage;
        damageData.SetDamageByType(meteor.DamageType, elementalDamage);
    }

    /// <summary>
    ///     Wholeness of Body deals 2d6 elemental damage in a large area round the monk, with a successful reflex save
    ///     halving the damage. Each Ki Focus adds 2d6 damage to a maximum of 8d6 elemental damage.
    /// </summary>
    private static void AugmentWholenessOfBody(NwCreature monk, CrashingMeteorData meteor)
    {
        WholenessOfBody.DoWholenessOfBody(monk);

        if (monk.Location == null) return;

        monk.ApplyEffect(EffectDuration.Instant, meteor.AoeVfx);
        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true,
                     ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Placeable))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (monk.IsReactionTypeFriendly(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.Fireball)!);

            bool hasEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
            bool hasImprovedEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

            SavingThrowResult savingThrowResult =
                creatureInShape.RollSavingThrow(SavingThrow.Reflex, meteor.Dc, meteor.SaveType, monk);

            int damageAmount = Random.Shared.Roll(6, meteor.DiceAmount);

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            Effect damageEffect = Effect.LinkEffects(
                Effect.Damage(damageAmount, meteor.DamageType),
                Effect.VisualEffect(meteor.DamageVfx));

            if (savingThrowResult == SavingThrowResult.Failure)
            {
                creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
                continue;
            }

            if (hasEvasion || hasImprovedEvasion)
            {
                creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
                continue;
            }

            creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
            creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
        }
    }

    /// <summary>
    ///     Ki Shout changes the damage from sonic to the chosen element. In addition, all enemies receive 10% vulnerability
    ///     to the element for three rounds, with every Ki Focus increasing it by 10%, to a maximum of 40% elemental damage
    ///     vulnerability.
    /// </summary>
    private static void AugmentKiShout(NwCreature monk, CrashingMeteorData meteor)
    {
        KiShout.DoKiShout(monk, meteor.DamageType, meteor.DamageVfx);

        if (monk.Location == null) return;

        foreach (NwGameObject obj in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            if (obj is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            Effect? elementalEffect = hostileCreature.ActiveEffects.FirstOrDefault(e => e.Tag == MeteorKiShoutTag);
            if (elementalEffect != null)
                hostileCreature.RemoveEffect(elementalEffect);

            elementalEffect = Effect.LinkEffects(
                Effect.DamageImmunityDecrease(meteor.DamageType, meteor.DamageVulnerability),
                Effect.VisualEffect(VfxType.DurCessateNegative)
            );

            hostileCreature.ApplyEffect(EffectDuration.Temporary, elementalEffect, NwTimeSpan.FromRounds(3));
        }
    }
}
