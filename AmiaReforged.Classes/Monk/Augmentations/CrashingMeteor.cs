using AmiaReforged.Classes.Monk.Constants;
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
    private static readonly Effect UseReflexVfx = Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse);
    public PathType PathType => PathType.CrashingMeteor;

    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData)
    {
        CrashingMeteorData meteor = GetCrashingMeteorData(monk);
        AugmentAxiomaticStrike(monk, attackData, meteor);
    }

    public void ApplyDamageAugmentation(NwCreature monk, TechniqueType technique, OnCreatureDamage damageData)
    {
        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        switch (technique)
        {
            case TechniqueType.StunningStrike:
                AugmentStunningStrike(monk, damageData, meteor);
                break;
            case TechniqueType.EagleStrike:
                EagleStrike.DoEagleStrike(monk, damageData);
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
        public Effect PulseVfx;
        public VfxType DamageVfx;
        public DamageType DamageType;
        public SavingThrowType SaveType;
        public int DamageVulnerability;
    }

    private static CrashingMeteorData GetCrashingMeteorData(NwCreature monk)
    {
        ElementalType elementalType = MonkUtils.GetElementalTypeVar(monk).Value;

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
                ElementalType.Fire => VfxType.FnfFireball,
                ElementalType.Water => MonkVfx.FnfFreezingSphere,
                ElementalType.Air => VfxType.FnfElectricExplosion,
                ElementalType.Earth => MonkVfx.FnfVitriolicSphere,
                _ => VfxType.FnfFireball
            }, RadiusSize.Large),
            PulseVfx = MonkUtils.ResizedVfx(elementalType switch
            {
                ElementalType.Fire => MonkVfx.ImpPulseFireChest,
                ElementalType.Water => MonkVfx.ImpPulseColdChest,
                ElementalType.Air => MonkVfx.ImpPulseAirChest,
                ElementalType.Earth => MonkVfx.ImpPulseEarthChest,
                _ => MonkVfx.ImpPulseFireChest
            }, RadiusSize.Medium),
            DamageVfx = elementalType switch
            {
                ElementalType.Fire => VfxType.ImpFlameS,
                ElementalType.Water => VfxType.ImpFrostS,
                ElementalType.Air => VfxType.ComHitElectrical,
                ElementalType.Earth => VfxType.ImpAcidS,
                _ => VfxType.ImpFlameS
            },
            DamageType = elementalType switch
            {
                ElementalType.Fire => DamageType.Fire,
                ElementalType.Water => DamageType.Cold,
                ElementalType.Air => DamageType.Electrical,
                ElementalType.Earth => DamageType.Acid,
                _ => DamageType.Fire
            },
            SaveType = elementalType switch
            {
                ElementalType.Fire => SavingThrowType.Fire,
                ElementalType.Water => SavingThrowType.Cold,
                ElementalType.Air => SavingThrowType.Electricity,
                ElementalType.Earth => SavingThrowType.Acid,
                _ => SavingThrowType.Fire
            },
            DamageVulnerability = MonkUtils.GetKiFocus(monk) switch
            {
                KiFocus.KiFocus1 => 10,
                KiFocus.KiFocus2 => 15,
                KiFocus.KiFocus3 => 20,
                _ => 5
            }
        };
    }

    /// <summary>
    ///     Stunning Strike deals 2d6 elemental damage in a large area around the target. The damage isn’t multiplied by
    ///     critical hits and a successful reflex save halves the damage. Each Ki Focus adds 2d6 to a maximum of 8d6 elemental
    ///     damage.
    /// </summary>
    private void AugmentStunningStrike(NwCreature monk, OnCreatureDamage damageData, CrashingMeteorData meteor)
    {
        StunningStrike.DoStunningStrike(damageData);

        if (damageData.Target.Location is not { } location) return;

        damageData.Target.ApplyEffect(EffectDuration.Instant, meteor.PulseVfx);

        foreach (NwCreature creature in location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Medium, true))
        {
            if (!monk.IsReactionTypeHostile(creature)) continue;
            int damageAmount = Random.Shared.Roll(6, meteor.DiceAmount);

            bool hasEvasion = creature.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
            bool hasImprovedEvasion = creature.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

            SavingThrowResult savingThrowResult =
                creature.RollSavingThrow(SavingThrow.Reflex, meteor.Dc, meteor.SaveType, monk);

            if ((hasEvasion || hasImprovedEvasion) && savingThrowResult == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, UseReflexVfx);
                continue;
            }

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            _ = ApplyAoeDamage(creature, monk, damageAmount, meteor.DamageType, meteor.DamageVfx);
        }
    }

    /// <summary>
    ///     Axiomatic Strike deals +1 bonus elemental damage, with an additional +1 for every Ki Focus,
    ///     to a maximum of +4 elemental damage.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData, CrashingMeteorData meteor)
    {
        AxiomaticStrike.DoAxiomaticStrike(monk, attackData);

        DamageData<short> damageData = attackData.DamageData;
        short elementalDamage = damageData.GetDamageByType(meteor.DamageType);

        int bonusDamage = meteor.BonusDamage;

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (elementalDamage == -1) bonusDamage++;

        elementalDamage += (short)bonusDamage;
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
            int damageAmount = Random.Shared.Roll(6, meteor.DiceAmount);
            if (nwObject is not NwCreature creature)
            {
                _ = ApplyAoeDamage(nwObject, monk, damageAmount, meteor.DamageType, meteor.DamageVfx);
                continue;
            }
            if (monk.IsReactionTypeFriendly(creature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creature, NwSpell.FromSpellType(Spell.Fireball)!);

            bool hasEvasion = creature.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
            bool hasImprovedEvasion = creature.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

            SavingThrowResult savingThrowResult =
                creature.RollSavingThrow(SavingThrow.Reflex, meteor.Dc, meteor.SaveType, monk);

            if ((hasEvasion || hasImprovedEvasion) && savingThrowResult == SavingThrowResult.Success)
            {
                creature.ApplyEffect(EffectDuration.Instant, UseReflexVfx);
                continue;
            }

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            _ = ApplyAoeDamage(creature, monk, damageAmount, meteor.DamageType, meteor.DamageVfx);
        }
    }

    private static async Task ApplyAoeDamage(NwGameObject targetObject, NwCreature monk, int damageAmount,
        DamageType damageType, VfxType damageVfx)
    {
        await monk.WaitForObjectContext();
        Effect damageEffect = Effect.LinkEffects(
            Effect.Damage(damageAmount, damageType),
            Effect.VisualEffect(damageVfx));

        targetObject.ApplyEffect(EffectDuration.Instant, damageEffect);
    }

    /// <summary>
    /// Ki Shout changes the damage from sonic to the chosen element. In addition, all enemies receive 5 %
    /// vulnerability to the element for three rounds, with every Ki Focus increasing it by 5 %, to a maximum of
    /// 20 % elemental damage vulnerability.
    /// </summary>
    private static void AugmentKiShout(NwCreature monk, CrashingMeteorData meteor)
    {
        KiShout.DoKiShout(monk, meteor.DamageType, meteor.DamageVfx);

        if (monk.Location == null) return;

        Effect elementalEffect = Effect.LinkEffects(Effect.DamageImmunityDecrease(meteor.DamageType, meteor.DamageVulnerability),
            Effect.VisualEffect(VfxType.DurCessateNegative));

        elementalEffect.SubType = EffectSubType.Extraordinary;
        elementalEffect.Tag = MeteorKiShoutTag;

        foreach (NwGameObject obj in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            if (obj is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            Effect? existingEffect = hostileCreature.ActiveEffects.FirstOrDefault(e => e.Tag == MeteorKiShoutTag);
            if (existingEffect != null)
                hostileCreature.RemoveEffect(existingEffect);

            hostileCreature.ApplyEffect(EffectDuration.Temporary, elementalEffect, NwTimeSpan.FromRounds(3));
        }
    }
}
