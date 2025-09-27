using Anvil.API;

namespace AmiaReforged.Classes.Associates.Bonuses;

public class CompanionBonuses(NwCreature owner, NwCreature associate)
{
    private readonly int _druidLevel = owner.GetClassInfo(ClassType.Druid)?.Level ?? 0;
    private readonly int _rangerLevel = owner.GetClassInfo(ClassType.Ranger)?.Level ?? 0;
    private static readonly NwFeat? EpicCompanionFeat = NwFeat.FromFeatId(1240);
    private int BonusLevel => _druidLevel + _rangerLevel;

    public void ApplyCompanionBonus()
    {
        associate.ApplyEffect(EffectDuration.Permanent, CompanionEffect());

        NwItem? leftClaw = associate.GetItemInSlot(InventorySlot.CreatureLeftWeapon);
        NwItem? rightClaw = associate.GetItemInSlot(InventorySlot.CreatureRightWeapon);
        NwItem? bite = associate.GetItemInSlot(InventorySlot.CreatureBiteWeapon);

        if (leftClaw != null)
            leftClaw.AddItemProperty(EnhancementBonus().ItemProperty, EffectDuration.Permanent);
        if (rightClaw != null)
            rightClaw.AddItemProperty(EnhancementBonus().ItemProperty, EffectDuration.Permanent);
        if (bite != null)
            bite.AddItemProperty(EnhancementBonus().ItemProperty, EffectDuration.Permanent);

        if (owner.IsPlayerControlled(out NwPlayer? player))
            SendCompanionBonusFeedback(player);

        if (EpicCompanionFeat == null || !owner.KnowsFeat(EpicCompanionFeat)) return;

        associate.ApplyEffect(EffectDuration.Permanent, EpicCompanionEffect());

        // Temp HP needs to be added separately because otherwise the rest of the effects are yeeted with it when damaged
        Effect hpBonus = Effect.TemporaryHitpoints(_bonuses.HpBonus);
        hpBonus.SubType = EffectSubType.Unyielding;
        associate.ApplyEffect(EffectDuration.Permanent, hpBonus);

        if (player != null)
            SendEpicCompanionBonusFeedback(player);
    }

    private Effect CompanionEffect()
    {
        Effect companionBonus = Effect.LinkEffects(
            NaturalArmorBonus().Effect,
            RegenerateBonus().Effect,
            AttackBonus().Effect,
            UniversalSaveBonus().Effect,
            DamageResistanceBonus().Effect,
            Effect.CutsceneGhost());

        companionBonus.SubType = EffectSubType.Unyielding;

        return companionBonus;
    }

    private (Effect Effect, int Bonus) NaturalArmorBonus()
    {
        int acBonus = BonusLevel / 2;

        return (Effect.ACIncrease(acBonus, ACBonus.Natural),  acBonus);
    }

    private (Effect Effect, int Bonus) RegenerateBonus()
    {
        int regenBonus = BonusLevel / 5 == 0 ? 1 : BonusLevel / 5;

        return (Effect.Regenerate(regenBonus, NwTimeSpan.FromRounds(1)), regenBonus);
    }

    private (Effect Effect, int Bonus) AttackBonus()
    {
        int attackBonus = BonusLevel / 5;

        return (Effect.AttackIncrease(attackBonus), attackBonus);
    }

    private (Effect Effect, int Bonus) UniversalSaveBonus()
    {
        int saveBonus = BonusLevel / 5;

        return (Effect.SavingThrowIncrease(SavingThrow.All, saveBonus), saveBonus);
    }

    private (Effect Effect, int Bonus) DamageResistanceBonus()
    {
        int damageResistanceBonus = BonusLevel switch
        {
            >= 21 => 15,
            >= 11 => 10,
            _ => 5
        };

        Effect damageResistance =
            Effect.LinkEffects(Effect.DamageResistance(DamageType.Bludgeoning, damageResistanceBonus),
                Effect.DamageResistance(DamageType.Piercing, damageResistanceBonus),
                Effect.DamageResistance(DamageType.Slashing, damageResistanceBonus));

        return (damageResistance, damageResistanceBonus);
    }

    private (ItemProperty ItemProperty, int Bonus) EnhancementBonus()
    {
        int enhancementBonus = BonusLevel / 5;

        return (ItemProperty.EnhancementBonus(enhancementBonus), enhancementBonus);
    }

    private void SendCompanionBonusFeedback(NwPlayer player)
    {
        string feedback =
            $"Companion Bonuses Added:\n" +
            $"Natural AC +{NaturalArmorBonus().Bonus}\n" +
            $"Weapon Enhancement +{EnhancementBonus().Bonus}\n" +
            $"Regeneration +{RegenerateBonus().Bonus}\n" +
            $"Attack Bonus +{AttackBonus().Bonus}\n" +
            $"Universal Save +{UniversalSaveBonus().Bonus}\n" +
            $"Physical Resistance +{DamageResistanceBonus().Bonus}";

        player.SendServerMessage(feedback.ColorString(ColorConstants.Olive));
    }

    private Effect EpicCompanionEffect()
    {
        Effect epicCompanionBonus = Effect.LinkEffects(
                Effect.AbilityIncrease(Ability.Constitution, _bonuses.ConstitutionBonus),
                Effect.AbilityIncrease(Ability.Dexterity, _bonuses.DexterityBonus),
                Effect.AbilityIncrease(Ability.Strength, _bonuses.StrengthBonus),
                _bonuses.Effects
        );

        epicCompanionBonus.SubType = EffectSubType.Unyielding;

        return epicCompanionBonus;
    }

    private record EpicCompanionBonus
    (
        int StrengthBonus,
        int ConstitutionBonus,
        int DexterityBonus,
        int HpBonus,
        Effect Effects
    );

    private readonly EpicCompanionBonus _bonuses = associate.AnimalCompanionType switch
    {
        AnimalCompanionCreatureType.Badger => new EpicCompanionBonus
        (
            StrengthBonus: 6,
            ConstitutionBonus: 6,
            DexterityBonus: 6,
            HpBonus: 200,
            Effects: Effect.SkillIncrease(Skill.Discipline!, 40)
        ),
        AnimalCompanionCreatureType.Wolf => new EpicCompanionBonus
        (
            StrengthBonus: 6,
            ConstitutionBonus: 6,
            DexterityBonus: 6,
            HpBonus: 200,
            Effects: Effect.LinkEffects(
                Effect.Concealment(50),
                Effect.Ultravision(),
                Effect.DamageIncrease(6, DamageType.Bludgeoning)
            )
        ),
        AnimalCompanionCreatureType.Bear => new EpicCompanionBonus
        (
            StrengthBonus: 6,
            ConstitutionBonus: 12,
            DexterityBonus: 6,
            HpBonus: 300,
            Effects: Effect.LinkEffects(
                Effect.DamageIncrease(10, DamageType.Bludgeoning),
                Effect.SkillIncrease(Skill.Discipline!, 50)
            )
        ),
        AnimalCompanionCreatureType.Boar => new EpicCompanionBonus
        (
            StrengthBonus: 6,
            ConstitutionBonus: 6,
            DexterityBonus: 6,
            HpBonus: 200,
            Effects: Effect.DamageIncrease(10, DamageType.Bludgeoning)
        ),
        AnimalCompanionCreatureType.Hawk => new EpicCompanionBonus
        (
            StrengthBonus: 10,
            ConstitutionBonus: 6,
            DexterityBonus: 4,
            HpBonus: 200,
            Effects: Effect.LinkEffects(
                Effect.SkillIncrease(Skill.Spot!, 13),
                Effect.Immunity(ImmunityType.Paralysis),
                Effect.Immunity(ImmunityType.Entangle),
                Effect.Immunity(ImmunityType.Slow),
                Effect.Immunity(ImmunityType.MovementSpeedDecrease),
                Effect.DamageIncrease(5, DamageType.Piercing)
            )
        ),
        AnimalCompanionCreatureType.Panther => new EpicCompanionBonus
        (
            StrengthBonus: 6,
            ConstitutionBonus: 8,
            DexterityBonus: 6,
            HpBonus: 200,
            Effects: Effect.LinkEffects(
                Effect.DamageImmunityIncrease(DamageType.Bludgeoning, 10),
                Effect.DamageImmunityIncrease(DamageType.Piercing, 10),
                Effect.DamageImmunityIncrease(DamageType.Slashing, 10)
            )
        ),
        AnimalCompanionCreatureType.Spider => new EpicCompanionBonus
        (
            StrengthBonus: 9,
            ConstitutionBonus: 6,
            DexterityBonus: 6,
            HpBonus: 200,
            Effects: Effect.LinkEffects(
                Effect.DamageIncrease(5, DamageType.Piercing),
                Effect.DamageIncrease(DamageBonus.Plus2d6, DamageType.Acid)
            )
        ),
        AnimalCompanionCreatureType.DireWolf => new EpicCompanionBonus
        (
            StrengthBonus: 10,
            ConstitutionBonus: 10,
            DexterityBonus: 10,
            HpBonus: 200,
            Effects: Effect.DamageIncrease(10, DamageType.Bludgeoning)
        ),
        AnimalCompanionCreatureType.DireRat => new EpicCompanionBonus
        (
            StrengthBonus: 8,
            ConstitutionBonus: 8,
            DexterityBonus: 8,
            HpBonus: 200,
            Effects: Effect.DamageResistance(DamageType.Fire, 10)
        ),
        _ => new EpicCompanionBonus(0, 0, 0, 0, Effect.VisualEffect(VfxType.None))
    };

    private void SendEpicCompanionBonusFeedback(NwPlayer player)
    {
        string feedback =
            $"\nEpic Companion Bonuses Added:\n" +
            $"Strength +{_bonuses.StrengthBonus}\n" +
            $"Dexterity +{_bonuses.DexterityBonus}\n" +
            $"Constitution +{_bonuses.ConstitutionBonus}\n" +
            $"Temporary HP +{_bonuses.HpBonus}\n";

        switch (owner.AnimalCompanionType)
        {
            case AnimalCompanionCreatureType.Badger:
                feedback += "Discipline +50";
                break;
            case AnimalCompanionCreatureType.Wolf:
                feedback += "Concealment +50%\n";
                feedback += "Ultravision\n";
                feedback += "Damage +6 (Bludgeoning)";
                break;
            case AnimalCompanionCreatureType.Bear:
                feedback += "Damage +10 (Bludgeoning)\n";
                feedback += "Discipline +50";
                break;
            case AnimalCompanionCreatureType.Boar:
                feedback += "Damage +10 (Bludgeoning)";
                break;
            case AnimalCompanionCreatureType.Hawk:
                feedback += "Spot +13\n";
                feedback += "Freedom of Movement\n";
                feedback += "Damage +5 (Piercing)";
                break;
            case AnimalCompanionCreatureType.Panther:
                feedback += "Physical Damage Immunity +10%";
                break;
            case AnimalCompanionCreatureType.Spider:
                feedback += "Damage +5 (Piercing)\n";
                feedback += "Damage +2d6 (Acid)";
                break;
            case AnimalCompanionCreatureType.DireWolf:
                feedback += "Damage +10 (Bludgeoning)";
                break;
            case AnimalCompanionCreatureType.DireRat:
                feedback += "Damage Resistance +10 (Fire)";
                break;
        }

        player.SendServerMessage(feedback.ColorString(ColorConstants.Olive));
    }
}
